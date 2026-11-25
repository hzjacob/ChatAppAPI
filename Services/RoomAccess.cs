namespace ChatAppTest.Services;

public static class RoomAccess
{
    public const string PublicRoomId = "public-messages";

    // "public-messages" is open to every logged-in user.
    // "private-{a}-{b}" is only open to users a and b.
    // otherUserId is the other participant of a private room (0 for the public room).
    public static bool CanAccessRoom(string? roomId, int userId, out long otherUserId)
    {
        otherUserId = 0;
        if (roomId == PublicRoomId)
            return true;

        var parts = roomId?.Split('-');
        if (parts is not { Length: 3 } || parts[0] != "private"
            || !int.TryParse(parts[1], out var a) || !int.TryParse(parts[2], out var b))
            return false;

        if (a == userId) { otherUserId = b; return true; }
        if (b == userId) { otherUserId = a; return true; }
        return false;
    }
}
