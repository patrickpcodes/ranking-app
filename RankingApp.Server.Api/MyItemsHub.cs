using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent; // for thread-safe dictionary

public class MyItemsHub : Hub
{
    // Let's store the listId for each connection in a static dictionary
    private static ConcurrentDictionary<string, string> connectionToListId
        = new ConcurrentDictionary<string, string>();

    public override async Task OnConnectedAsync()
    {
        // Read listId from query string
        var httpContext = Context.GetHttpContext();
        var listId = httpContext?.Request.Query["listId"].ToString();

        if( !string.IsNullOrEmpty( listId ) )
        {
            // Store it in the dictionary for later use
            connectionToListId[Context.ConnectionId] = listId;
            Console.WriteLine( $"Connection {Context.ConnectionId} requested listId: {listId}" );
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync( Exception? exception )
    {
        // Clean up the dictionary entry
        connectionToListId.TryRemove( Context.ConnectionId, out _ );
        await base.OnDisconnectedAsync( exception );
    }

    public async Task RequestNextItem()
    {
        // Look up the listId for this connection
        if( !connectionToListId.TryGetValue( Context.ConnectionId, out var listId ) )
        {
            // If we didn't find it, bail out or handle error
            await Clients.Caller.SendAsync( "ReceiveItem", "No list id associated with this connection" );
            return;
        }

        // Retrieve the items from your DB by listId. E.g.:
        var items = GetItemsFromDb( listId );

        // You need to track which item index each connection is on – 
        // so maybe store that in another dictionary
        // Just for example, let's say we always pick the first item
        // or the next item. We'll keep it simple.

        // Here I'm just going to pretend we have a method to get the "next" item.
        var nextItem = GetNextItemForConnection( listId, Context.ConnectionId );

        if( nextItem != null )
        {
            await Clients.Caller.SendAsync( "ReceiveItem", nextItem );
        }
        else
        {
            // No more items
            await Clients.Caller.SendAsync( "ReceiveItem", "No more items!" );
        }
    }

    public async Task SubmitInputForItem( string userInput )
    {
        // Again, use the stored listId to know which list to associate input with
        if( connectionToListId.TryGetValue( Context.ConnectionId, out var listId ) )
        {
            // do something with userInput + listId
            // like SaveInputForList(listId, userInput);
        }

        await Clients.Caller.SendAsync( "InputReceived", userInput );
    }

    // -------------------------------------
    // Just placeholders for your real logic
    // -------------------------------------
    private IEnumerable<string> GetItemsFromDb( string listId )
    {
        // you'd do EF or Dapper calls or something here
        return new List<string> { "item1", "item2", "item3" };
    }

    private string? GetNextItemForConnection( string listId, string connectionId )
    {
        // you'd store progress per connection, or per user
        // then fetch the "next" item
        return "someNextItem";
    }
}
