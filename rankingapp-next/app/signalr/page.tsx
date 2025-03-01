"use client";

import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

export default function Home() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(
    null
  );
  const [currentItem, setCurrentItem] = useState(null);
  const [userInput, setUserInput] = useState("");

  useEffect(() => {
    // Suppose you have a param 'listId'
    const listId = "12345";

    // Include it in the hub URL as a query parameter
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`https://localhost:7071/myItemsHub?listId=${listId}`)
      .withAutomaticReconnect()
      .build();

    newConnection
      .start()
      .then(() => {
        console.log("Connected to SignalR!");

        // Set up your handlers
        newConnection.on("ReceiveItem", (item) => setCurrentItem(item));

        setConnection(newConnection);

        // Request the first item
        newConnection.invoke("RequestNextItem");
      })
      .catch(console.error);
  }, []);

  const handleSubmit = async () => {
    if (connection) {
      // You might call "SubmitInputForItem" with user input
      await connection.invoke("SubmitInputForItem", userInput);
      // Then get the next item
      await connection.invoke("RequestNextItem");
    }
    setUserInput("");
  };

  if (!connection) {
    return <div>Connecting...</div>;
  }

  return (
    <div>
      <h1>Current Item: {currentItem ?? "No item yet!"}</h1>
      <input
        type="text"
        value={userInput}
        onChange={(e) => setUserInput(e.target.value)}
      />
      <button onClick={handleSubmit}>Submit &amp; Next</button>
    </div>
  );
}
