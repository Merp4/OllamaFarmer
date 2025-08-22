// Extends generated OpenAPI types with server-exposed fields not yet in schema

declare module "../api/schema" {
  namespace components {
    namespace schemas {
      interface AppMessageDto {
        toolCallId?: string | null;
        toolCallName?: string | null;
        toolCallArgs?: Record<string, unknown> | null;
        isCompletedFunctionCall?: boolean;
        isCompletedToolResult?: boolean;
      }
    }
  }
}
