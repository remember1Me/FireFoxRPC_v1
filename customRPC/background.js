let ws;
const reconnectInterval = 5000; // Time (in ms) to wait before trying to reconnect

// Function to initialize WebSocket connection
function initializeWebSocket() {
    ws = new WebSocket("ws://localhost:8080");

    ws.onopen = () => {
        console.log("Connected to C# app");
    };

    ws.onerror = (error) => {
        console.error("WebSocket error:", error);
    };

    ws.onclose = () => {
        console.log("Disconnected from C# app. Attempting to reconnect...");
        setTimeout(initializeWebSocket, reconnectInterval); // Attempt reconnection
    };
}

// Initialize the WebSocket connection when the extension starts
initializeWebSocket();

// Function to send tab data to the C# app
function sendTabData(tab) {
    if (!ws || ws.readyState !== WebSocket.OPEN) {
        console.log("WebSocket is not open. Reconnection in progress...");
        return; // Don't send data if the connection is not open
    }

    const data = {
        title: tab.title || "Unknown Page",
        url: tab.url,
        timestamp: Date.now()
    };

    ws.send(JSON.stringify(data));
}

// Listen for active tab change
browser.tabs.onActivated.addListener((activeInfo) => {
    browser.tabs.get(activeInfo.tabId, sendTabData);
});

// Listen for tab updates (page load complete)
browser.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (changeInfo.status === "complete") sendTabData(tab);
});
