using Cranberry.Errors;

namespace Cranberry.Types;

public class WebsocketClient(Interpreter interpreter, string url) : IMemberAccessible {
	private readonly CSignal MessageReceived = new(interpreter);
	private readonly string Url = url;

	public object GetMember(object? member) {
		if (member is string m) {
			if (m == "message_received") return MessageReceived;

			// if (m == "run") return new InternalFunction((_, _) => {
			// 	using ClientWebSocket client = new ClientWebSocket();
			// 	try {
			// 		await client.ConnectAsync(new Uri(Url), CancellationToken.None);
			// 		Console.WriteLine("Connected to WebSocket server.");
			//
			// 		// Send a message
			// 		string messageToSend = "Hello from C# client!";
			// 		byte[] buffer = Encoding.UTF8.GetBytes(messageToSend);
			// 		await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
			// 		Console.WriteLine($"Sent: {messageToSend}");
			//
			// 		// Receive messages
			// 		while (client.State == WebSocketState.Open) {
			// 			byte[] receiveBuffer = new byte[1024];
			// 			WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
			//
			// 			if (result.MessageType == WebSocketMessageType.Text) {
			// 				string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
			// 				Console.WriteLine($"Received: {receivedMessage}");
			// 			} else if (result.MessageType == WebSocketMessageType.Close) {
			// 				await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
			// 				Console.WriteLine("Server requested close.");
			// 				break;
			// 			}
			// 		}
			// 	} catch (WebSocketException ex) {
			// 		Console.WriteLine($"WebSocket error: {ex.Message}");
			// 	} catch (Exception ex) {
			// 		Console.WriteLine($"General error: {ex.Message}");
			// 	}
			// 	
			// 	return null;
			// });
			
			if (m == "send") return MessageReceived;
		}

		throw new RuntimeError($"Tried to get unknown member `{member}` on WebsocketClient");
	}
}