using System.Globalization;
using System.Text.RegularExpressions;
using ShopDocsV2.Domain;

namespace ShopDocsV2.Application;

/// <summary>
/// Field-by-field port of the original app's ordx.js. QuestionSet is accepted for interface symmetry
/// with the other Application builders, but (matching the original, which never consulted QUESTIONS
/// either) the field ids read here — cabinet_style, cabinet_finishes, cabinet_hardware, countertops,
/// sinks, appliance_package, notes, etc. — are the fixed question/itemField ids baked into the
/// shop's real questions.json, not schema-driven.
/// </summary>
public sealed partial class OrdxExportService : IOrdxExportService
{
    private const string ParameterTypeComment =
        "<!--Measurement|Meas|M|Degrees|Deg|D|Integer|Int|I|Boolean|Bool|B|Decimal|Dec|D|Text|T|Currency|Cur|C-->";

    // Boilerplate CV "Standard" job configuration: internal CV profile IDs and default measurements
    // (interior/exterior finish profile numbers, cabinet heights/depths, filler/scribe rules, etc.)
    // that don't come from the job spec. Carried over verbatim from the original app's real 2026 export.
    private static readonly (string Name, string Type, string Value)[] DefaultJobAttributes =
    {
        ("_CONFIG", "T", "Standard"),
        ("_INTF", "I", "972"),
        ("_INTFS", "D", "0.3"),
        ("_EXTF", "I", "972"),
        ("_EXTFS", "D", "0.3"),
        ("_MPRF", "I", "613"),
        ("_LRPRF", "I", "115"),
        ("_SCRPRF", "I", "108"),
        ("_BBPRF", "I", "116"),
        ("_CRPRF", "I", "114"),
        ("_CAPRF", "I", "110"),
        ("_APPRF", "I", "113"),
        ("_CLPRF", "I", "112"),
        ("_BH", "M", "34.5"),
        ("_BD", "M", "24"),
        ("_UH", "M", "36"),
        ("_UD", "M", "12"),
        ("_TH", "M", "90"),
        ("_TD", "M", "24"),
        ("_VH", "M", "34.5"),
        ("_VD", "M", "21"),
        ("_WH", "M", "108"),
        ("_SH", "M", "18"),
        ("_WT", "M", "4.5"),
        ("_VERDTM", "M", "54"),
        ("_CTTYP", "I", "1"),
        ("_CTPRF", "I", "616"),
        ("_CTF", "I", "2"),
        ("_CRH", "M", "36"),
        ("_FLNO", "I", "3"),
        ("_FLSTYLE", "I", "0"),
        ("_FLMARTA", "M", "1.5"),
        ("_FLMARTB", "M", "2"),
        ("_FLMARBA", "M", "1.5"),
        ("_FLMARBB", "M", "2"),
        ("_FLTMAR", "M", "0.25"),
        ("_FLSPACE", "M", "0.75"),
        ("_FLRAMP", "M", "0.25"),
        ("_FLDZ", "M", "0.375"),
        ("_FLDX", "M", "0.25"),
        ("_FLTOOL", "I", "0"),
        ("_CEOH", "M", "0"),
        ("_LRI", "M", "1.5"),
    };

    private static readonly string[] DoorPositions = { "All", "Base", "Drawer", "Upper", "BaseEP", "UpperEP", "TallEP" };

    public string BuildOrdxXml(Job job, QuestionSet questionSet, DateTime? createdAt = null)
    {
        var namedRooms = job.Rooms.Where(r => !string.IsNullOrWhiteSpace(r.Name)).OrderBy(r => r.SortOrder).ToList();
        var primaryRoom = FirstAnsweredRoom(job);
        var roomsXml = string.Join("\n", namedRooms.Select((room, index) => BuildRoomXml(room, index, primaryRoom)));

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!-- CV ORDX Order XML File -->
            <Job Created="{FormatCreated(createdAt ?? DateTime.Now)}">
              <ProductVersion>2026</ProductVersion>
              <Unit>inches</Unit>
            {BuildPropertiesXml(job, primaryRoom)}
              <Rooms>
            {roomsXml}
              </Rooms>
            </Job>

            """;
    }

    // ---------------- Primary room selection ----------------

    /// <summary>
    /// A room "has any content" if a scalar answer is non-blank or a list question has at least one item.
    /// This is the same predicate ISpecFormattingService.IsAnswered uses for scalars/lists, but note it is
    /// deliberately looser than IsAnswered for lists (any item counts here, even one with every field blank) —
    /// kept as a separate predicate, exactly mirroring the original's distinct hasAnyAnswer() vs isAnswered().
    /// </summary>
    private static bool HasAnyAnswer(Room room)
    {
        if (room.Answers.Values.Any(v => !v.IsBlank)) return true;
        if (room.ListAnswers.Values.Any(items => items.Count > 0)) return true;
        return false;
    }

    private static Room? FirstAnsweredRoom(Job job) =>
        job.Rooms.OrderBy(r => r.SortOrder).FirstOrDefault(HasAnyAnswer);

    // ---------------- Field accessors (fixed question/itemField ids, not schema-driven) ----------------

    private static string GetText(Room? room, string questionId)
    {
        if (room is null) return "";
        return room.Answers.TryGetValue(questionId, out var v) && !v.IsBlank ? v.DisplayText : "";
    }

    private static bool GetBool(Room? room, string questionId) =>
        room is not null && room.Answers.TryGetValue(questionId, out var v) && v.Kind == AnswerValueKind.Bool && (v.Bool ?? false);

    private static List<RoomListItem> GetList(Room? room, string questionId)
    {
        if (room is null) return new List<RoomListItem>();
        return room.ListAnswers.TryGetValue(questionId, out var items) ? items.OrderBy(i => i.SortOrder).ToList() : new List<RoomListItem>();
    }

    private static RoomListItem? FirstListItem(Room? room, string questionId) => GetList(room, questionId).FirstOrDefault();

    private static string GetItemText(RoomListItem? item, string fieldId)
    {
        if (item is null) return "";
        return item.Fields.TryGetValue(fieldId, out var v) && !v.IsBlank ? v.DisplayText : "";
    }

    // ---------------- XML helpers ----------------

    [GeneratedRegex("&")]
    private static partial Regex AmpPattern();
    [GeneratedRegex("<")]
    private static partial Regex LtPattern();
    [GeneratedRegex(">")]
    private static partial Regex GtPattern();
    [GeneratedRegex("-{2,}")]
    private static partial Regex DoubleDashPattern();
    [GeneratedRegex("-$")]
    private static partial Regex TrailingDashPattern();

    private static string XmlEscape(string? value)
    {
        var s = value ?? "";
        s = AmpPattern().Replace(s, "&amp;");
        s = LtPattern().Replace(s, "&lt;");
        s = GtPattern().Replace(s, "&gt;");
        return s;
    }

    private static string XmlComment(string text)
    {
        var safe = DoubleDashPattern().Replace(text, "- -");
        safe = TrailingDashPattern().Replace(safe, "- ");
        return $"<!--{safe}-->";
    }

    private static string Tag(string name, string? value) => $"<{name}>{XmlEscape(value)}</{name}>";

    private static string FormatCreated(DateTime date) => date.ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);

    private static string MapConstructionStyle(string? cabinetStyle) =>
        cabinetStyle == "Frameless" ? "32mm" : "OCS Standard Finished Bottom";

    // ---------------- Parameter / Attribute helpers ----------------

    private static string BuildParameterXml(string name, string type, string value) => $"""
        <Parameter>
                  {Tag("Name", name)}
                  {Tag("Type", type)}{ParameterTypeComment}
                  {Tag("Value", value)}
                </Parameter>
        """;

    private static string BuildAttributesXml(IEnumerable<(string Name, string Type, string Value)> parameters)
    {
        var body = string.Join("\n        ", parameters.Select(p => BuildParameterXml(p.Name, p.Type, p.Value)));
        return $"""
                  <Attributes>
                    {body}
                  </Attributes>
            """;
    }

    private static string BuildParametersXml(IEnumerable<(string Name, string Type, string Value)> parameters)
    {
        var body = string.Join("\n        ", parameters.Select(p => BuildParameterXml(p.Name, p.Type, p.Value)));
        return $"""
                <Parameters>
                {body}
                </Parameters>
            """;
    }

    // ---------------- Properties: Job ----------------

    private static string BuildJobInformationXml(Job job)
    {
        string NameBlock(string nameTag) => $"""

                      {Tag("Name", job.CustomerName)}
                      {Tag("Address1", job.Address)}
                      {Tag("City", "")}
                      {Tag("State", "")}
                      {Tag("Zip", "")}
                      {Tag("Email", job.CustomerEmail)}
                      {Tag("Phone", job.CustomerPhone)}
                    </{nameTag}>
            """.TrimEnd('\n', '\r');

        return $"""
                  <Information>
                    <Job>
                      {Tag("Name", job.CustomerName)}
                      {Tag("Description", "")}
                    </Job>
                    <Customer>{NameBlock("Customer")}
                    <ShipTo>{NameBlock("ShipTo")}
                  </Information>
            """;
    }

    // ---------------- Properties: Room (job-level defaults) ----------------

    private static string BuildGlobalRoomXml(Room? primaryRoom)
    {
        var finishValue = GetItemText(FirstListItem(primaryRoom, "cabinet_finishes"), "finish");

        return $"""
                <Room>
                  <Finishes>
                    <Finish>
                      {Tag("Interior", finishValue)}
                      {Tag("Exterior", finishValue)}
                    </Finish>
                  </Finishes>
            {BuildAttributesXml(new[] { ("ROOM", "I", "1") })}
                </Room>
            """;
    }

    // ---------------- Properties: Cabinet ----------------

    private static string BuildDoorPositionXml(string position, string style, string material)
    {
        // Drawer fronts are typically slab profiles and carry no inside edge detail.
        var insideEdge = position == "Drawer" ? "" : $"\n          {Tag("InsideEdgeDetail", "OCS Shaker Stile and Rail")}";
        return $"""
                    <{position}>
                      {Tag("Style", style)}
                      {Tag("Material", material)}
                      {Tag("OutsideEdgeDetail", "Shaker Outside Profile")}{insideEdge}
                      {Tag("Catalog", "OCS Standard Door Configs")}
                    </{position}>
            """;
    }

    private static string BuildDoorsXml(Room? room, string wood)
    {
        // Door Style is passed through as free text from the job spec (Shaker / Raised Panel).
        var doorStyle = GetText(room, "cabinet_door_style");
        var material = !string.IsNullOrEmpty(wood) ? $"OCS {wood} Doors" : "OCS MDF Doors";
        return string.Join("\n", DoorPositions.Select(position => BuildDoorPositionXml(position, doorStyle, material)));
    }

    private static string BuildCabinetXml(Room? primaryRoom)
    {
        var finish = FirstListItem(primaryRoom, "cabinet_finishes");
        var hardware = FirstListItem(primaryRoom, "cabinet_hardware");
        var construction = MapConstructionStyle(GetText(primaryRoom, "cabinet_style"));
        var wood = GetItemText(finish, "wood");
        var pull = GetItemText(hardware, "catalogItem");
        if (string.IsNullOrEmpty(pull)) pull = "OCS Standard Pulls";
        var softClose = GetBool(primaryRoom, "soft_close");

        var drawerBox = softClose ? "OCS Standard Softclose Drawer" : "OCS Standard Drawer";
        var hinge = softClose ? "OCS Soft Close Overlay" : "OCS 1/2 Regular Close Overlay";
        var guide = softClose ? "OCS Soft Close Full Extension" : "OCS Pro 600 Series";

        var standardMaterial = !string.IsNullOrEmpty(wood) ? $"OCS {wood}/Prefinished" : "OCS Paint/Prefinished";
        var exposedMaterial = !string.IsNullOrEmpty(wood) ? $"OCS {wood}/{wood}" : "OCS Paint/Paint";

        return $"""
                <Cabinet>
                  <Construction>
                    {Tag("Cabinet", construction)}
                    {Tag("DrawerBox", drawerBox)}
                    {Tag("RollOut", "OCS Standard Rollout")}
                  </Construction>
                  <Materials>
                    <BaseMaterialSchedules>
                      {Tag("Standard", standardMaterial)}
                      {Tag("ExposedInterior", exposedMaterial)}
                    </BaseMaterialSchedules>
                    <UpperMaterialSchedules>
                      {Tag("Standard", standardMaterial)}
                      {Tag("ExposedInterior", exposedMaterial)}
                    </UpperMaterialSchedules>
                  </Materials>
                  <Hardware>
                    {Tag("PullSchedule", pull)}
                    {Tag("HingeSchedule", hinge)}
                    {Tag("GuideSchedule", guide)}
                    {Tag("SlidingDoorRailSchedule", "Sliding Door Rail")}
                  </Hardware>
                  <Doors>
            {BuildDoorsXml(primaryRoom, wood)}
                  </Doors>
                </Cabinet>
            """;
    }

    private static string BuildMoldingXml(string wood) => $"""
            <Molding>
              {Tag("Material", string.IsNullOrEmpty(wood) ? "Paint Grade" : wood)}
              {Tag("Crown", "Shaker Crown")}
              {Tag("LightRail", "OCS Light Rail EWLR12")}
              {Tag("Scribe", "OCS Scribe")}
              {Tag("BaseBoard", "Toe Kick")}
              {Tag("ChairRail", "3in Chair Rail")}
              {Tag("Casing", "2-1/4 Window Casing")}
              {Tag("Ceiling", "3-5/8 Ceiling Crown")}
            </Molding>
        """;

    private static string BuildPropertiesXml(Job job, Room? primaryRoom)
    {
        var wood = GetItemText(FirstListItem(primaryRoom, "cabinet_finishes"), "wood");

        return $"""
              <Properties>
                <Job>
            {BuildJobInformationXml(job)}
            {BuildAttributesXml(DefaultJobAttributes)}
                </Job>
            {BuildGlobalRoomXml(primaryRoom)}
            {BuildCabinetXml(primaryRoom)}
                <Closets>
                </Closets>
            {BuildMoldingXml(wood)}
              </Properties>
            """;
    }

    // ---------------- Rooms ----------------

    private static string BuildRoomGeneralXml(Room room) => $"""
                  <General>
                    {Tag("Name", room.Name)}
                    {Tag("Description", "")}
                    <Type>Room</Type><!--Room|Project|Plan|FloorLevel|Phase-->
                    {Tag("Material", "OCS Basic Room")}
                  </General>
        """;

    private static string BuildRoomFinishesOverrideXml(Room room, Room? primaryRoom)
    {
        if (room.Id == primaryRoom?.Id) return "";
        var finishValue = GetItemText(FirstListItem(room, "cabinet_finishes"), "finish");
        var primaryFinishValue = GetItemText(FirstListItem(primaryRoom, "cabinet_finishes"), "finish");
        if (string.IsNullOrEmpty(finishValue) || finishValue == primaryFinishValue) return "";

        return $"""

                      <Finishes>
                        <Finish>
                          {Tag("Interior", finishValue)}
                          {Tag("Exterior", finishValue)}
                        </Finish>
                      </Finishes>
            """.TrimEnd('\n', '\r');
    }

    private static string BuildRoomCabinetOverrideXml(Room room, Room? primaryRoom)
    {
        if (room.Id == primaryRoom?.Id)
        {
            return """
                        <Cabinet>
                        </Cabinet>
                """;
        }

        var wood = GetItemText(FirstListItem(room, "cabinet_finishes"), "wood");
        var primaryWood = GetItemText(FirstListItem(primaryRoom, "cabinet_finishes"), "wood");
        var doorStyle = GetText(room, "cabinet_door_style");
        var primaryDoorStyle = GetText(primaryRoom, "cabinet_door_style");

        var woodChanged = !string.IsNullOrEmpty(wood) && wood != primaryWood;
        var doorStyleChanged = !string.IsNullOrEmpty(doorStyle) && doorStyle != primaryDoorStyle;

        if (!woodChanged && !doorStyleChanged)
        {
            return """
                        <Cabinet>
                        </Cabinet>
                """;
        }

        var effectiveWood = !string.IsNullOrEmpty(wood) ? wood : primaryWood;
        var standardMaterial = $"OCS {effectiveWood}/Prefinished";
        var exposedMaterial = $"OCS {effectiveWood}/{effectiveWood}";

        var materialsXml = woodChanged
            ? $"""

                          <Materials>
                            <BaseMaterialSchedules>
                              {Tag("Standard", standardMaterial)}
                              {Tag("ExposedInterior", exposedMaterial)}
                            </BaseMaterialSchedules>
                            <UpperMaterialSchedules>
                              {Tag("Standard", standardMaterial)}
                              {Tag("ExposedInterior", exposedMaterial)}
                            </UpperMaterialSchedules>
                          </Materials>
                """.TrimEnd('\n', '\r')
            : "";

        var doorsXml = $"""

                      <Doors>
            {BuildDoorsXml(room, effectiveWood)}
                      </Doors>
            """.TrimEnd('\n', '\r');

        return $"""
                        <Cabinet>{materialsXml}{doorsXml}
                        </Cabinet>
            """;
    }

    private static string BuildRoomMoldingOverrideXml(Room room, Room? primaryRoom)
    {
        if (room.Id == primaryRoom?.Id) return "";
        var wood = GetItemText(FirstListItem(room, "cabinet_finishes"), "wood");
        var primaryWood = GetItemText(FirstListItem(primaryRoom, "cabinet_finishes"), "wood");
        if (string.IsNullOrEmpty(wood) || wood == primaryWood) return "";

        return $"""

                    <Molding>
                      {Tag("Material", wood)}
                    </Molding>
            """.TrimEnd('\n', '\r');
    }

    private static string BuildRoomExtrasComment(Room room)
    {
        var lines = new List<string>();

        foreach (var f in GetList(room, "cabinet_finishes").Skip(1))
        {
            var parts = new[] { GetItemText(f, "name"), GetItemText(f, "wood"), GetItemText(f, "finish") }.Where(p => p != "").ToList();
            if (parts.Count > 0) lines.Add($"Additional finish zone: {string.Join(" / ", parts)}");
        }

        foreach (var h in GetList(room, "cabinet_hardware"))
        {
            var parts = new[] { GetItemText(h, "name"), GetItemText(h, "catalogItem"), GetItemText(h, "color"), GetItemText(h, "model") }.Where(p => p != "").ToList();
            if (parts.Count > 0) lines.Add($"Hardware: {string.Join(" / ", parts)}");
        }

        foreach (var c in GetList(room, "countertops"))
        {
            var parts = new[] { GetItemText(c, "material"), GetItemText(c, "color"), GetItemText(c, "edge_profile"), GetItemText(c, "backsplash") }.Where(p => p != "").ToList();
            if (parts.Count > 0) lines.Add($"Countertop: {string.Join(" / ", parts)}");
        }

        foreach (var s in GetList(room, "sinks"))
        {
            var parts = new[] { GetItemText(s, "sink_type"), GetItemText(s, "model"), GetItemText(s, "faucet_style") }.Where(p => p != "").ToList();
            if (parts.Count > 0) lines.Add($"Sink: {string.Join(" / ", parts)}");
        }

        foreach (var ap in GetList(room, "appliance_package"))
        {
            var parts = new[] { GetItemText(ap, "name"), GetItemText(ap, "model") }.Where(p => p != "").ToList();
            if (parts.Count > 0) lines.Add($"Appliance: {string.Join(" / ", parts)}");
        }

        foreach (var n in GetList(room, "notes"))
        {
            var text = GetItemText(n, "text");
            if (text != "") lines.Add($"Note: {text}");
        }

        if (lines.Count == 0) return "";
        return $"\n      {XmlComment(" From job spec, not represented natively in ORDX:\n      " + string.Join("\n      ", lines) + "\n      ")}";
    }

    private static string BuildRoomXml(Room room, int index, Room? primaryRoom) => $"""
            <Room>
              <Perspective>
              </Perspective>
              <RoomProperties>
                <Room>
        {BuildRoomGeneralXml(room)}{BuildRoomFinishesOverrideXml(room, primaryRoom)}
        {BuildParametersXml(new[] { ("ROOM", "I", (index + 1).ToString(CultureInfo.InvariantCulture)) })}
                </Room>
        {BuildRoomCabinetOverrideXml(room, primaryRoom)}
                <Closets>
                </Closets>{BuildRoomMoldingOverrideXml(room, primaryRoom)}
              </RoomProperties>{BuildRoomExtrasComment(room)}
            </Room>
        """;
}
