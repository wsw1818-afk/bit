#!/usr/bin/env node
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import WebSocket from "ws";

const UNITY_WS_URL = process.env.UNITY_WS_URL || "ws://localhost:8090/McpUnity";

interface UnityResponse {
  id: string;
  result?: any;
  error?: any;
}

class UnityMCPBridge {
  private ws: WebSocket | null = null;
  private requestId = 0;
  private pendingRequests = new Map<string, { resolve: (value: any) => void; reject: (error: any) => void }>();

  async connect() {
    return new Promise<void>((resolve, reject) => {
      console.error(`Connecting to Unity at ${UNITY_WS_URL}...`);
      this.ws = new WebSocket(UNITY_WS_URL);

      this.ws.on('open', () => {
        console.error("Connected to Unity WebSocket");
        resolve();
      });

      this.ws.on('message', (data) => {
        try {
          const response = JSON.parse(data.toString()) as UnityResponse;
          const pending = this.pendingRequests.get(response.id);
          if (pending) {
            this.pendingRequests.delete(response.id);
            if (response.error) {
              pending.reject(response.error);
            } else {
              pending.resolve(response.result);
            }
          }
        } catch (error) {
          console.error("Error parsing Unity response:", error);
        }
      });

      this.ws.on('error', (error) => {
        console.error("WebSocket error:", error);
        reject(error);
      });

      this.ws.on('close', () => {
        console.error("WebSocket connection closed");
      });
    });
  }

  async callUnity(method: string, params: any = {}): Promise<any> {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
      throw new Error("WebSocket not connected");
    }

    this.requestId++;
    const id = this.requestId.toString();
    
    const request = {
      id,
      method,
      params
    };

    return new Promise((resolve, reject) => {
      this.pendingRequests.set(id, { resolve, reject });
      this.ws!.send(JSON.stringify(request));
      
      // Timeout after 30 seconds
      setTimeout(() => {
        if (this.pendingRequests.has(id)) {
          this.pendingRequests.delete(id);
          reject(new Error("Request timeout"));
        }
      }, 30000);
    });
  }

  disconnect() {
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
  }
}

const unityBridge = new UnityMCPBridge();

// Create MCP server
const server = new McpServer({
  name: "unity-mcp-server",
  version: "1.0.0",
});

// Connect to Unity when server starts
async function initialize() {
  try {
    await unityBridge.connect();
    console.error("Unity MCP Bridge initialized successfully");
  } catch (error) {
    console.error("Failed to connect to Unity:", error);
    process.exit(1);
  }
}

// Game testing tools
server.tool(
  "test_game_flow",
  {
    gamePath: z.string().optional().describe("Path to Unity project"),
  },
  async ({ gamePath }) => {
    try {
      console.error("Starting game flow test...");
      
      // 1. Load Gameplay scene
      console.error("Loading Gameplay scene...");
      await unityBridge.callUnity("load_scene", {
        scenePath: "Assets/Scenes/Gameplay.unity",
        loadMode: "Single"
      });

      // 2. Wait for scene to load
      await new Promise(resolve => setTimeout(resolve, 2000));

      // 3. Start game
      console.error("Starting game...");
      await unityBridge.callUnity("start_game", {
        songId: "SimpleTest"
      });

      // 4. Wait for game to start
      await new Promise(resolve => setTimeout(resolve, 1000));

      // 5. Simulate some gameplay
      console.error("Simulating gameplay...");
      const lanes = [0, 1, 2, 3];
      for (let i = 0; i < 10; i++) {
        const lane = lanes[Math.floor(Math.random() * lanes.length)];
        await unityBridge.callUnity("simulate_note_hit", {
          lane: lane,
          accuracy: Math.random() * 100
        });
        await new Promise(resolve => setTimeout(resolve, 500));
      }

      // 6. Get game stats
      console.error("Getting game stats...");
      const stats = await unityBridge.callUnity("get_game_stats", {});

      return {
        content: [
          {
            type: "text",
            text: `Game flow test completed successfully!\n\nStats: ${JSON.stringify(stats, null, 2)}`
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: "text",
            text: `Game flow test failed: ${error}`
          }
        ],
        isError: true
      };
    }
  }
);

server.tool(
  "load_scene",
  {
    scenePath: z.string().describe("Path to Unity scene"),
    loadMode: z.enum(["Single", "Additive"]).optional().default("Single"),
  },
  async ({ scenePath, loadMode }) => {
    try {
      const result = await unityBridge.callUnity("load_scene", { scenePath, loadMode });
      return {
        content: [
          {
            type: "text",
            text: `Scene loaded: ${scenePath}`
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: "text",
            text: `Failed to load scene: ${error}`
          }
        ],
        isError: true
      };
    }
  }
);

server.tool(
  "start_game",
  {
    songId: z.string().describe("Song ID to play"),
    difficulty: z.enum(["Easy", "Normal", "Hard"]).optional().default("Normal"),
  },
  async ({ songId, difficulty }) => {
    try {
      const result = await unityBridge.callUnity("start_game", { songId, difficulty });
      return {
        content: [
          {
            type: "text",
            text: `Game started with song: ${songId} (${difficulty})`
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: "text",
            text: `Failed to start game: ${error}`
          }
        ],
        isError: true
      };
    }
  }
);

server.tool(
  "simulate_input",
  {
    lane: z.number().min(0).max(3).describe("Lane number (0-3)"),
    accuracy: z.number().min(0).max(100).optional().default(100),
  },
  async ({ lane, accuracy }) => {
    try {
      const result = await unityBridge.callUnity("simulate_note_hit", { lane, accuracy });
      return {
        content: [
          {
            type: "text",
            text: `Simulated input on lane ${lane} with ${accuracy}% accuracy`
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: "text",
            text: `Failed to simulate input: ${error}`
          }
        ],
        isError: true
      };
    }
  }
);

server.tool(
  "get_game_stats",
  {},
  async () => {
    try {
      const stats = await unityBridge.callUnity("get_game_stats", {});
      return {
        content: [
          {
            type: "text",
            text: `Game Stats: ${JSON.stringify(stats, null, 2)}`
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: "text",
            text: `Failed to get game stats: ${error}`
          }
        ],
        isError: true
      };
    }
  }
);

server.tool(
  "pause_game",
  {},
  async () => {
    try {
      await unityBridge.callUnity("pause_game", {});
      return {
        content: [
          {
            type: "text",
            text: "Game paused"
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: "text",
            text: `Failed to pause game: ${error}`
          }
        ],
        isError: true
      };
    }
  }
);

server.tool(
  "resume_game",
  {},
  async () => {
    try {
      await unityBridge.callUnity("resume_game", {});
      return {
        content: [
          {
            type: "text",
            text: "Game resumed"
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: "text",
            text: `Failed to resume game: ${error}`
          }
        ],
        isError: true
      };
    }
  }
);

// Initialize and start server
async function main() {
  await initialize();
  
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Unity MCP Server running on stdio");
}

main().catch((error) => {
  console.error("Fatal error:", error);
  process.exit(1);
});