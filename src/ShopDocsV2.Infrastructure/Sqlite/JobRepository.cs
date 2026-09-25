using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.Infrastructure.Sqlite;

public sealed class JobRepository(SqliteConnectionFactory connectionFactory) : IJobRepository
{
    public async Task<Guid> CreateJobAsync(Job job)
    {
        if (job.Id == Guid.Empty)
        {
            job.Id = Guid.NewGuid();
        }
        job.UpdatedAt = DateTime.Now;

        using var connection = connectionFactory.Create();
        using var transaction = connection.BeginTransaction();

        await InsertJobRowAsync(connection, transaction, job);
        await InsertRoomsGraphAsync(connection, transaction, job);

        transaction.Commit();
        return job.Id;
    }

    public async Task<Job?> GetJobAsync(Guid id)
    {
        using var connection = connectionFactory.Create();

        var jobRow = await connection.QuerySingleOrDefaultAsync<JobRow>(
            "SELECT id, customer_name, customer_phone, customer_email, address, date_created, due_date, updated_at FROM jobs WHERE id = @id",
            new { id = id.ToString() });
        if (jobRow is null)
        {
            return null;
        }

        var job = MapJob(jobRow);

        var roomRows = (await connection.QueryAsync<RoomRow>(
            "SELECT id, name, sort_order FROM rooms WHERE job_id = @jobId ORDER BY sort_order",
            new { jobId = id.ToString() })).ToList();

        var roomsById = roomRows.Select(MapRoom).ToDictionary(r => r.Id);
        job.Rooms.AddRange(roomRows.Select(r => roomsById[Guid.Parse(r.id)]));

        if (roomRows.Count == 0)
        {
            return job;
        }

        var roomIds = roomRows.Select(r => r.id).ToList();

        var answerRows = await connection.QueryAsync<AnswerRow>(
            "SELECT room_id, question_id, value_kind, value_text FROM room_answers WHERE room_id IN @roomIds",
            new { roomIds });
        foreach (var row in answerRows)
        {
            roomsById[Guid.Parse(row.room_id)].Answers[row.question_id] = MapAnswerValue(row.value_kind, row.value_text);
        }

        var listItemRows = (await connection.QueryAsync<ListItemRow>(
            "SELECT id, room_id, question_id, sort_order FROM room_list_items WHERE room_id IN @roomIds ORDER BY sort_order",
            new { roomIds })).ToList();

        var listItemsById = new Dictionary<Guid, RoomListItem>();
        foreach (var row in listItemRows)
        {
            var item = new RoomListItem { Id = Guid.Parse(row.id), SortOrder = row.sort_order };
            listItemsById[item.Id] = item;

            var room = roomsById[Guid.Parse(row.room_id)];
            if (!room.ListAnswers.TryGetValue(row.question_id, out var items))
            {
                items = [];
                room.ListAnswers[row.question_id] = items;
            }
            items.Add(item);
        }

        if (listItemRows.Count > 0)
        {
            var listItemIds = listItemRows.Select(r => r.id).ToList();
            var valueRows = await connection.QueryAsync<ListItemValueRow>(
                "SELECT list_item_id, field_id, value_kind, value_text FROM room_list_item_values WHERE list_item_id IN @listItemIds",
                new { listItemIds });
            foreach (var row in valueRows)
            {
                listItemsById[Guid.Parse(row.list_item_id)].Fields[row.field_id] = MapAnswerValue(row.value_kind, row.value_text);
            }
        }

        return job;
    }

    public async Task SaveJobAsync(Job job)
    {
        job.UpdatedAt = DateTime.Now;

        using var connection = connectionFactory.Create();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            """
            UPDATE jobs SET customer_name=@CustomerName, customer_phone=@CustomerPhone, customer_email=@CustomerEmail,
                address=@Address, date_created=@DateCreated, due_date=@DueDate, updated_at=@UpdatedAt
            WHERE id=@Id
            """,
            ToJobParams(job), transaction);

        await connection.ExecuteAsync("DELETE FROM rooms WHERE job_id = @jobId", new { jobId = job.Id.ToString() }, transaction);
        await InsertRoomsGraphAsync(connection, transaction, job);

        transaction.Commit();
    }

    public async Task DeleteJobAsync(Guid id)
    {
        using var connection = connectionFactory.Create();
        await connection.ExecuteAsync("DELETE FROM jobs WHERE id = @id", new { id = id.ToString() });
    }

    public async Task<IReadOnlyList<JobSummary>> ListJobsAsync()
    {
        using var connection = connectionFactory.Create();
        var rows = await connection.QueryAsync<JobRow>(
            "SELECT id, customer_name, customer_phone, customer_email, address, date_created, due_date, updated_at FROM jobs ORDER BY updated_at DESC");
        return rows
            .Select(r => new JobSummary(Guid.Parse(r.id), r.customer_name, r.address, r.date_created, r.due_date, r.updated_at))
            .ToList();
    }

    public async Task<Guid> DuplicateJobAsync(Guid sourceJobId)
    {
        var source = await GetJobAsync(sourceJobId)
            ?? throw new InvalidOperationException($"Job {sourceJobId} not found.");

        var clone = source.Duplicate(DateTime.Now);

        using var connection = connectionFactory.Create();
        using var transaction = connection.BeginTransaction();
        await InsertJobRowAsync(connection, transaction, clone);
        await InsertRoomsGraphAsync(connection, transaction, clone);
        transaction.Commit();

        return clone.Id;
    }

    private static Task InsertJobRowAsync(SqliteConnection connection, SqliteTransaction transaction, Job job) =>
        connection.ExecuteAsync(
            """
            INSERT INTO jobs (id, customer_name, customer_phone, customer_email, address, date_created, due_date, updated_at)
            VALUES (@Id, @CustomerName, @CustomerPhone, @CustomerEmail, @Address, @DateCreated, @DueDate, @UpdatedAt)
            """,
            ToJobParams(job), transaction);

    private static async Task InsertRoomsGraphAsync(SqliteConnection connection, SqliteTransaction transaction, Job job)
    {
        foreach (var room in job.Rooms)
        {
            await connection.ExecuteAsync(
                "INSERT INTO rooms (id, job_id, name, sort_order) VALUES (@Id, @JobId, @Name, @SortOrder)",
                new { Id = room.Id.ToString(), JobId = job.Id.ToString(), room.Name, room.SortOrder },
                transaction);

            foreach (var (questionId, value) in room.Answers)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO room_answers (room_id, question_id, value_kind, value_text) VALUES (@RoomId, @QuestionId, @Kind, @Text)",
                    new { RoomId = room.Id.ToString(), QuestionId = questionId, Kind = value.Kind.ToString(), Text = ToStoredText(value) },
                    transaction);
            }

            foreach (var (questionId, items) in room.ListAnswers)
            {
                foreach (var item in items)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO room_list_items (id, room_id, question_id, sort_order) VALUES (@Id, @RoomId, @QuestionId, @SortOrder)",
                        new { Id = item.Id.ToString(), RoomId = room.Id.ToString(), QuestionId = questionId, item.SortOrder },
                        transaction);

                    foreach (var (fieldId, value) in item.Fields)
                    {
                        await connection.ExecuteAsync(
                            "INSERT INTO room_list_item_values (list_item_id, field_id, value_kind, value_text) VALUES (@ItemId, @FieldId, @Kind, @Text)",
                            new { ItemId = item.Id.ToString(), FieldId = fieldId, Kind = value.Kind.ToString(), Text = ToStoredText(value) },
                            transaction);
                    }
                }
            }
        }
    }

    private static string? ToStoredText(AnswerValue value) => value.Kind switch
    {
        AnswerValueKind.Text => value.Text,
        AnswerValueKind.Number => value.Number?.ToString(CultureInfo.InvariantCulture),
        AnswerValueKind.Bool => "1",
        _ => null
    };

    private static AnswerValue MapAnswerValue(string kind, string? text) => Enum.Parse<AnswerValueKind>(kind) switch
    {
        AnswerValueKind.Number => AnswerValue.Of(double.Parse(text!, CultureInfo.InvariantCulture)),
        AnswerValueKind.Bool => AnswerValue.Of(true),
        _ => AnswerValue.Of(text)
    };

    private static object ToJobParams(Job job) => new
    {
        Id = job.Id.ToString(),
        job.CustomerName,
        job.CustomerPhone,
        job.CustomerEmail,
        job.Address,
        job.DateCreated,
        job.DueDate,
        job.UpdatedAt
    };

    private static Job MapJob(JobRow row) => new()
    {
        Id = Guid.Parse(row.id),
        CustomerName = row.customer_name,
        CustomerPhone = row.customer_phone,
        CustomerEmail = row.customer_email,
        Address = row.address,
        DateCreated = row.date_created,
        DueDate = row.due_date,
        UpdatedAt = row.updated_at
    };

    private static Room MapRoom(RoomRow row) => new()
    {
        Id = Guid.Parse(row.id),
        Name = row.name,
        SortOrder = row.sort_order
    };

    // Plain mutable classes, not records: Dapper materializes SQLite's TEXT-stored dates via property
    // setters (which convert), but tries to bind positional-record constructors by exact raw column
    // type (string) and fails before any DateTime conversion happens.
    private sealed class JobRow
    {
        public string id { get; set; } = "";
        public string customer_name { get; set; } = "";
        public string? customer_phone { get; set; }
        public string? customer_email { get; set; }
        public string? address { get; set; }
        public DateTime date_created { get; set; }
        public DateTime? due_date { get; set; }
        public DateTime updated_at { get; set; }
    }

    private sealed class RoomRow
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public int sort_order { get; set; }
    }

    private sealed class AnswerRow
    {
        public string room_id { get; set; } = "";
        public string question_id { get; set; } = "";
        public string value_kind { get; set; } = "";
        public string? value_text { get; set; }
    }

    private sealed class ListItemRow
    {
        public string id { get; set; } = "";
        public string room_id { get; set; } = "";
        public string question_id { get; set; } = "";
        public int sort_order { get; set; }
    }

    private sealed class ListItemValueRow
    {
        public string list_item_id { get; set; } = "";
        public string field_id { get; set; } = "";
        public string value_kind { get; set; } = "";
        public string? value_text { get; set; }
    }
}
