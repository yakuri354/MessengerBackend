
using System;
using MessengerBackend.Sockets.Models;

namespace MessengerBackend.Sockets
{
    public class SocketMessageSerializer
    {
        // Socket message structure:
        // Starts with 1 byte, this byte indicates socket message type
        // If user wants to connect, after this goes JWT api access token, terminated with 0x00
        // After that, socket is opened and user can send other messages

        // public static SocketMessage Serialize(RawSocketMessage message)
        // {
        //     var msgCommand = message.Message[0];
        //     
        //     return new ParsedSocketMessage(Enum.GetName(typeof(SocketMessageCommand), msgCommand), _getIsInbound(msgCommand), message.);
        // }

        private static bool _getIsInbound(byte command)
        {
            return command < 0x80;
        }
        
    }
    public enum SocketMessageCommand // from 0x00 to 0x50
    {
        // Client commands ( from 0x00 to 0x30 )
        Authorization = 0x01,
        Register = 0x02,
        MessageToAnotherUser = 0x10,
        // Server commands ( from 0x30 to 0x50 )
        InboundMessage = 0x50
    }

    public enum SocketMessage
    {
        
    }
}