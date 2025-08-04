import { useState } from "react";
import { $queryClient } from "../api/api";
import { toast } from "react-toastify";
import type { components } from "../api/schema";

//type ChatMessage = components["schemas"]["AppMessageDto"];
type ChatRole = components["schemas"]["NullableOfChatRole"];
type AppChat = components["schemas"]["AppChatDto"];

// Helper function to extract system message
function getSystemMessage(messages?: { role?: ChatRole; content?: string | null; images?: string[] | null }[]): string {
    if (!messages || messages.length === 0) return "";
    const firstSystemMessage = messages[0];
    if (firstSystemMessage.role !== "system") return "";
    return firstSystemMessage.content ?? "";
}

export function useModelOperations(chatId: string | undefined, data?: AppChat) {
    const [newModelName, setNewModelName] = useState<string>(data?.name ?? "My Chat");

    const { mutateAsync: mutateChatModelAsync } = $queryClient.useMutation(
        "post", 
        "/api/AppChat/{id}/create-model", 
        {    
            onMutate: () => {
                toast.info("Saving model as " + newModelName + "...");
            },
            onSuccess: (response) => {
                toast.success("Model saved as " + response?.model);
            },
            onError: (error) => {
                toast.error("Error saving model: " + JSON.stringify(error, [], 2));
            },
        }
    );

    function saveNewModel(): void {
        if (!data) return;

        if (!data.model) {
            toast.error("Model not found");
            return;
        }
        
        mutateChatModelAsync({
            body: {
                model: data.model,
                newModelName: newModelName,
                template: "",
                systemMessage: getSystemMessage(data.messages),
                topP: data.options?.topP ?? undefined, 
                temperature: data.options?.temperature ?? undefined,
            },
            params: { path: { id: chatId! } }
        });
    }

    return {
        newModelName,
        setNewModelName,
        saveNewModel
    };
}
