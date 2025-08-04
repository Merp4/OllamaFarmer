// Central type definitions for the application
import type { components } from "./schema";

// API Schema types
export type McpServer = components["schemas"]["McpServer"];
export type McpTool = components["schemas"]["McpTool"];
export type ChatServerDto = components["schemas"]["ChatServerDto"];
export type ChatMessage = components["schemas"]["AppMessageDto"];
export type AppChatDto = components["schemas"]["AppChatDto"];
export type AppChatDetails = components["schemas"]["AppChatDetails"];
export type AppChatDetailsResponse = components["schemas"]["AppChatDetailsResponse"];
export type CreateChatRequest = components["schemas"]["CreateChatDto"];
export type SendMessageDto = components["schemas"]["SendMessageDto"];
export type ModelCapabilities = components["schemas"]["ModelCapabilities"];
export type AppChatOptions = components["schemas"]["AppChatOptions"];
export type ChatRole = components["schemas"]["ChatRole"];
export type NullableOfChatRole = components["schemas"]["NullableOfChatRole"];

