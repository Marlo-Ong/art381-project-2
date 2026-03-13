using System;

[Serializable]
public class CreateRoomRequest
{
    public string ownerName;
    public string roomName;
    public int totalTokens;
    public string createdAt;
    public string updatedAt;
}
