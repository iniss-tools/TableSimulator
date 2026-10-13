namespace TableSimulator.Mcp;

/// <summary>Pokyny servera MCP pre model - strucny opis simulatora (anglicky ako popisy nastrojov), veta na riadok.</summary>
public static class McpInstructions
{
    public const string Text =
        """
        TableSimulator emulates passenger information boards (departure and arrival displays) driven by INISS, a railway station information system.
        INISS talks to boards over lines: each line is one connection (TCP, UDP, named pipe or serial port) with a line number, and boards on a line have an address.
        A board id is "line-address", e.g. "1-12".
        Board families: elen (LED matrix), chars (LCD character displays), flaps (split-flap displays), amplifier (ELSVO audio amplifier switching).
        Line status "connected" means INISS is connected; "lost" means INISS disconnected and the line waits for it.
        """;
}
