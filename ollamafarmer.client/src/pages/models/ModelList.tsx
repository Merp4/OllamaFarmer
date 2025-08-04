import { faArrowDown, faArrowsRotate, faRobot, faXmark, faCommentDots, faEdit } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { $queryClient } from "../../api/api";
import { Modal, ModalHeader, ModalBody, ModalFooter, Button, Form, FormGroup, Input, Label } from "reactstrap";
import { useState } from "react";
import { toast } from "react-toastify";
import CapabilitiesBadges from "../../components/CapabilitiesBadges";
import CreateChatModal from "../chat/components/CreateChatModal";
import ChatServerSelector from "../../components/ChatServerSelector";
import LoadingPanel from "../../components/LoadingPanel";
import { formatFileSize } from "../../utils/fileUtils";
import { useDialogContext } from "../../hooks/useDialogContext";
import EditModelModal from "./components/EditModelModal";
import type { components } from "../../api/schema";

type ChatModelDto = components["schemas"]["ChatModelDto"];

  
function ModelList() {

    const pageSize = 20;
    const [selectedServerId, setSelectedServerId] = useState<string>("");
    const dialogs = useDialogContext();

    const { data, error, fetchNextPage, hasNextPage, isFetching, isLoading, refetch } = $queryClient.useInfiniteQuery("get", "/api/ChatModel/details/all", { 
        params: { query: { serverId: selectedServerId || undefined, pageSize: pageSize } } 
    }, {
        getNextPageParam: (lastPage: { cursor: number | undefined; filteredCount: number | undefined; }) =>
            pageSize * ((lastPage.cursor ?? 0) + 1) >= (lastPage.filteredCount ?? 0)
                ? undefined : (lastPage.cursor ?? 0) + 1,
        getPreviousPageParam: (firstPage: { cursor: number | undefined; }) => firstPage.cursor ?? 0,
        initialPageParam: 0,
        enabled: !!selectedServerId,
    });

    const [createChatModalIsOpen, setCreateChatModalIsOpen] = useState(false);
    const [editModelModalIsOpen, setEditModelModalIsOpen] = useState(false);
    const [selectedModelForEdit, setSelectedModelForEdit] = useState<ChatModelDto | null>(null);

    const handleEditModel = (modelDetails: { chatModel?: ChatModelDto }) => {
        if (modelDetails.chatModel) {
            setSelectedModelForEdit(modelDetails.chatModel);
            setEditModelModalIsOpen(true);
        }
    };

    const handleSaveEditedModel = async (updatedModel: Partial<ChatModelDto>) => {
        if (!selectedModelForEdit?.id) {
            toast.error("Update Error: No model selected for editing");
            return;
        }

        try {
            await updateModelAsync({
                params: { path: { id: selectedModelForEdit.id } },
                body: updatedModel as ChatModelDto
            });
            
            setEditModelModalIsOpen(false);
            setSelectedModelForEdit(null);
            toast.success(`Model "${updatedModel.name || selectedModelForEdit.name}" updated successfully`);
            refetch(); // Refresh the model list
        } catch (error) {
            console.error('Failed to update model:', error);
            toast.error("Update Failed: Failed to update the model. Please try again.");
        }
    };

    const handleDeleteModel = (modelDetails: { chatModel?: { id?: string; name?: string | null } }) => {
        if (!modelDetails.chatModel?.id) {
            toast.error("Delete Error: Model ID not found");
            return;
        }

        dialogs.showDangerConfirmDialog(
            "Delete Model",
            `Are you sure you want to delete the model "${modelDetails.chatModel?.name}"? This will remove both the local database entry and the model from the Ollama server. This action cannot be undone.`,
            async () => {
                try {
                    await deleteModelAsync({
                        params: { path: { id: modelDetails.chatModel!.id! } }
                    });
                    toast.success(`Model "${modelDetails.chatModel?.name}" deleted successfully`);
                    refetch(); // Refresh the model list
                } catch (error) {
                    console.error('Failed to delete model:', error);
                    
                    // Extract error message from API response if available
                    let errorMessage = "Failed to delete the model. Please try again.";
                    
                    if (error && typeof error === 'object') {
                        const apiError = error as { response?: { data?: string | { message?: string } }; message?: string };
                        if (apiError.response?.data) {
                            if (typeof apiError.response.data === 'string') {
                                errorMessage = apiError.response.data;
                            } else if (apiError.response.data.message) {
                                errorMessage = apiError.response.data.message;
                            }
                        } else if (apiError.message) {
                            errorMessage = apiError.message;
                        }
                    }
                    
                    // Show error toast instead of dialog for consistency
                    toast.error(`Delete failed: ${errorMessage}`);
                }
            }
        );
    };

    const placeholderRow = <tr>
        {[...Array(5)].map((_, i) =>
            <td key={i} className="placeholder-glow">
                <span className="placeholder col-6 m-2 bg-primary"></span>
            </td>
        )}
    </tr>;

    const errorFragment = error && !isFetching &&
        <div className="d-inline-flex card bg-danger-subtle" aria-hidden="true">
            <div className="card-body">
                <h5 className="card-title">Error</h5>
                <p className="card-text">Failed to retrieve data, response: <code>{JSON.stringify(error)}</code></p>
            </div>
        </div>;

    const noDataFragment = !data && !isFetching &&
        <div className="card bg-warning-subtle" aria-hidden="true">
            <div className="card-body">
                <h5 className="card-title">No data</h5>
                <p className="card-text">No data available.</p>
            </div>
        </div>;


    const tableDataFragment = data &&
        <div className="p-1">
            <table className="table table-striped table-hover" aria-labelledby="tableLabel">
                <thead className="">
                    <tr className="">
                        <th className="">Model</th>
                        <th className="col">Description</th>
                        <th className="col text-center">Properties</th>
                        <th className="text-end">Pulled</th>
                        <th className=""></th>
                    </tr>
                </thead>
                <tbody >
                    {isLoading && placeholderRow}
                    {data.pages.map(page => {
                        return page.data?.map((modelDetails) => {
                            return (
                                <tr key={modelDetails.chatModel?.id}>
                                    <td className="">
                                        {modelDetails.chatModel?.imageUrl 
                                            && <img src={modelDetails.chatModel?.imageUrl} alt={modelDetails.chatModel?.name ?? ""} />
                                        || <FontAwesomeIcon icon={faRobot} className={"me-1 " + (modelDetails.chatModel?.isRunning ? "text-success" : (modelDetails.chatModel?.isLocal ? "text-light" : "text-muted"))} size="sm" />}
                                        <span className="ms-1">{modelDetails.chatModel?.name}</span>
                                    </td>
                                    <td className="col text-truncate">{modelDetails.chatModel?.description}</td>
                                    <td className="col">
                                    <div className="">
                                        {modelDetails.chatModel?.isLocal ||
                                        (<>
                                            <button disabled={isFetching || !selectedServerId} type="button" className="btn btn-sm btn-secondary" aria-label="Pull Model" onClick={() => {
                                                setModelName(modelDetails.chatModel?.name ?? "");
                                                pullModelAsync({ params: { path: { modelName: modelDetails.chatModel?.name ?? "" }, query: { serverId: selectedServerId } } });
                                            }}><FontAwesomeIcon icon={faArrowDown} className="me-2 " size="sm" />Pull</button>
                                        </>)}
                                        {modelDetails.chatModel?.parameterSize && (<span className="badge ms-2 bg-primary bg-opacity-50">{modelDetails.chatModel?.parameterSize}</span>)}
                                        {<span className="badge ms-2 bg-primary bg-opacity-50">{formatFileSize(modelDetails.chatModel?.size, 2)}</span>}
                                        {modelDetails.apiModel?.details?.family && (<span className="badge ms-2 bg-body text-light">{modelDetails.apiModel?.details?.family}</span>)}
                                        {modelDetails.apiModel?.details?.quantizationLevel && (<span className="badge ms-2 bg-body text-light">{modelDetails.apiModel?.details?.quantizationLevel}</span>)}
                                    </div>
                                    <div className="mt-2 ms-3">
                                        <CapabilitiesBadges capabilities={modelDetails.chatModel?.capabilities} />
                                    </div>
                                    </td>
                                    <td className="text-end">
                                        {modelDetails.chatModel?.createdAt && (
                                            <>
                                                {new Date(modelDetails.chatModel?.createdAt).toLocaleDateString()}
                                            </>
                                        ) || <span className="text-muted">Unknown</span>}
                                    </td>
                                    <td className="text-end">
                                        <button 
                                            type="button" 
                                            className="btn btn-sm ms-2 btn-primary" 
                                            aria-label="New Chat" 
                                            disabled={!selectedServerId}
                                            onClick={() => { 
                                                setModelName(modelDetails.chatModel?.name ?? ""); 
                                                setCreateChatModalIsOpen(true); 
                                            }}
                                        >
                                            <FontAwesomeIcon icon={faCommentDots} className="me-2 " size="sm" />New Chat</button>
                                            <button 
                                                type="button" 
                                                className="btn btn-sm ms-2 btn-warning" 
                                                aria-label="Synchronise Model" 
                                                disabled={!selectedServerId}
                                                onClick={() => refreshModelCapabilitiesAsync({ 
                                                    params: { 
                                                        query: { 
                                                            serverId: selectedServerId
                                                        } 
                                                    } 
                                                })}
                                            >
                                                <FontAwesomeIcon icon={faArrowsRotate} className="me-2 " size="sm" />Sync Capabilities</button>
                                        <button 
                                            type="button" 
                                            className="btn btn-sm ms-2 btn-secondary" 
                                            aria-label="Edit Model" 
                                            onClick={() => handleEditModel(modelDetails)}
                                        >
                                            <FontAwesomeIcon icon={faEdit} className="me-2 " size="sm" />Edit</button>
                                        <button type="button" className="btn btn-sm ms-2 btn-danger" aria-label="Delete Model" onClick={() => handleDeleteModel(modelDetails)}>
                                            <FontAwesomeIcon icon={faXmark} className="me-2 " size="sm" />Delete</button>
                                    </td>
                                </tr>
                            );
                        });
                    }
                    )}
                    {(isFetching || isLoading)
                        && placeholderRow
                        && placeholderRow
                        && placeholderRow
                        && placeholderRow}
                    {data.pages.length === 0 || data.pages[0].filteredCount === 0 && <tr>
                        <td colSpan={5} className="text-center text-muted">No models found. {data.pages[0].filteredCount}</td>
                    </tr>}
                </tbody>
            </table>
            {(hasNextPage && !isFetching) && <div className="d-flex flex-row mt-1 justify-content-center">
                <div className="d-flex h-100 text-center mt-3">
                    <button disabled={!hasNextPage || isFetching} type="button" className="btn btn-sm btn-secondary"
                        aria-label="Load more" onClick={() => fetchNextPage()}>
                        <FontAwesomeIcon icon={faArrowDown} className={"me-2 " + (isFetching ? "refresh-rotate" : "")} size="sm" />
                        Load more</button>
                </div>
            </div>}
        </div>;


    const [isOpen, setIsOpen] = useState(false);
    const toggle = () => setIsOpen(!isOpen);

    const [modelName, setModelName] = useState("");
    // const [modelDescription, setModelDescription] = useState("");
    // const [modelUrl, setModelUrl] = useState("");
    // const [modelImageUrl, setModelImageUrl] = useState("");
    // const [modelTags, setModelTags] = useState("");

    const { mutateAsync: pullModelAsync } = $queryClient.useMutation("get", "/api/ChatModel/pull/{modelName}", {
        params:{ path: { modelName: modelName }, query: { serverId: selectedServerId } },
        onMutate() {
            //console.log("onMutate", variables);
            setIsOpen(false);
        },
        onSuccess(data, variables) {
            console.log("onSuccess", data, variables);
            toast.success(`Model ${variables.params.path.modelName} pull queued.`);
        },
        onError(error, variables) {
            console.log("onError", error, variables);
            toast.error(`Model ${variables.params.path.modelName} pull failed.`);
        }
    });
    const {mutateAsync: refreshModelCapabilitiesAsync, isPending: refreshPending} = $queryClient.useMutation("get", "/api/ChatModel/refresh-capabilities", {
        onMutate() {
            //console.log("onMutate", variables);
        },
        onSuccess() {    
            // need delayed refetch to allow the server to update the model capabilities
            setTimeout(() => {
                refetch();
            }, 2000);
            toast.success(`Model capabilities refreshed.`);
        }
        ,
        onError() {
            refetch();
            toast.error(`Model capabilities refresh failed.`);
        }
    });
    
    const { mutateAsync: updateModelAsync } = $queryClient.useMutation("put", "/api/ChatModel/{id}", {
        onMutate() {
            // Optional: Add optimistic updates here
        },
        onSuccess() {
            // Handled in the calling function
        },
        onError(error) {
            console.error("Model update error:", error);
        }
    });

    const { mutateAsync: deleteModelAsync } = $queryClient.useMutation("delete", "/api/ChatModel/{id}", {
        onMutate() {
            // Optional: Add optimistic updates here
        },
        onSuccess() {
            // Handled in the calling function
        },
        onError(error) {
            console.error("Model delete error:", error);
        }
    });

    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">Models</h1>

                <div className="d-flex flex-row p-1 mb-3">
                    <div className="me-3">
                        <ChatServerSelector
                            selectedServerId={selectedServerId}
                            onServerChange={setSelectedServerId}
                            placeholder={undefined}
                            
                            style={{ minWidth: "200px" }}
                        />
                    </div>
                </div>

                {selectedServerId && <div className="d-flex flex-row p-1">
                    <button 
                        type="button" 
                        className="btn btn-primary me-3" 
                        aria-label="Pull new model" 
                        onClick={toggle}
                    >
                        <FontAwesomeIcon icon={faArrowDown} className="me-2 " />Pull New</button>
                    <button 
                        type="button" 
                        disabled={refreshPending} 
                        className="btn btn-warning me-3" 
                        aria-label="Synchronise all capabilities" 
                        onClick={() => refreshModelCapabilitiesAsync({ params: { query: { serverId: selectedServerId } } })}
                    >
                        <FontAwesomeIcon icon={faArrowsRotate} className={"me-2 " + (refreshPending ? "refresh-rotate" : "")} />Synchronise All Capabilities</button>
                    {/* <button type="button" className="btn btn-primary ms-3" aria-label="New chat" onClick={() => {}}>
                        <FontAwesomeIcon icon={faPlus} className="me-2 " />New</button> */}
                </div>}
            </div>
            <div className="container-fluid">
                {!selectedServerId && (
                    <div className="m-2 p-2" aria-hidden="true">
                            <p className="">Select a chat server to view and manage models.</p>
                    </div>
                )}
                {selectedServerId && (errorFragment
                    || isLoading && <LoadingPanel />     
                    || noDataFragment
                    || tableDataFragment)}
            </div>
           


            {createChatModalIsOpen && <CreateChatModal 
                isOpen={createChatModalIsOpen} 
                toggle={() => setCreateChatModalIsOpen(false)} 
                model={modelName} 
                serverId={selectedServerId}
            />}

            <EditModelModal 
                isOpen={editModelModalIsOpen} 
                onClose={() => setEditModelModalIsOpen(false)} 
                onSave={handleSaveEditedModel}
                model={selectedModelForEdit}
            />

           {isOpen && <Modal isOpen={isOpen} toggle={toggle} {...{ className: "modal-lg", fullscreen: false, backdrop: 'static',
                autoFocus: true, fade: true,
                modalTransition: { timeout: 300 }, backdropTransition: { timeout: 150 },
                onClosed: () => {}, onOpened: () => {}, onExit: () => {} }} scrollable unmountOnClose returnFocusAfterClose centered>
            <ModalHeader toggle={toggle} >Pull New Model</ModalHeader>
            <ModalBody>
                <div className="container-fluid">
                    <p>
                        Models available for download:
                        <a href="https://ollama.com/search" target="_blank" rel="noreferrer" className="d-inline-block m-3">
                            <img src="https://ollama.com/public/ollama.png" alt="Ollama" className="d-inline-block rounded-circle bg-white text-dark p-1 m-2" style={{ maxWidth: "3rem", maxHeight: "3rem" }} />
                            Ollama Library
                        </a>
                    </p>
                </div>
                <Form>
                    <FormGroup>
                        <Label for="modelName">Ollama library model name</Label>
                        <Input required type="text" onChange={(e) => {setModelName(e.target.value)}} id="modelName" placeholder="deepseek-r1:7b" />
                    </FormGroup>
                    <FormGroup>
                        <Label for="modelDescription">Model Description</Label>
                        <Input type="text" 
                            // onChange={(e) => {setModelDescription(e.target.value)}} 
                            id="modelDescription" placeholder="Enter model description" />
                    </FormGroup>
                    <FormGroup>
                        <Label for="modelUrl">Model URL</Label>
                        <Input type="text" 
                            // onChange={(e) => {setModelUrl(e.target.value)}} 
                            id="modelUrl" placeholder="https://ollama.com/" />
                    </FormGroup>
                    <FormGroup>
                        <Label for="modelImageUrl">Model Image URL</Label>
                        <Input type="text" 
                            // onChange={(e) => {setModelImageUrl(e.target.value)}} 
                            id="modelImageUrl" placeholder="https://ollama.com/ollama.png" />
                    </FormGroup>
                    <FormGroup>
                        <Label for="modelTags">Model Tags</Label>
                        <Input type="text" 
                            // onChange={(e) => {setModelTags(e.target.value)}} 
                            id="modelTags" placeholder="Enter model tags" />
                    </FormGroup>
                    </Form>
            </ModalBody>
            <ModalFooter>
                    <Button 
                        color="primary" 
                        disabled={!selectedServerId || !modelName}
                        onClick={() => {pullModelAsync({ params: { path: { modelName }, query: { serverId: selectedServerId } } })}}
                    >
                        <FontAwesomeIcon icon={faArrowDown} className="me-2 small" />
                        Pull
                    </Button>{' '}
                    <Button color="secondary" onClick={toggle}>
                        <FontAwesomeIcon icon={faXmark} className="me-2 small" />
                        Cancel
                    </Button>
            </ModalFooter>
        </Modal>}
        </div>
    );
}

export default ModelList;


/*
import { useState, useEffect } from 'react';

function StreamingComponent() {
  const [messages, setMessages] = useState([]);
  
  useEffect(() => {
    let abortController = new AbortController();
    
    async function fetchStream() {
      try {
        const response = await fetch('https://your-api/stream-endpoint', {
          signal: abortController.signal
        });
        
        if (!response.body) {
          throw new Error('ReadableStream not supported');
        }
        
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        
        while (true) {
          const { value, done } = await reader.read();
          if (done) break;
          
          // Decode and parse the chunk (assuming JSON items separated by newlines)
          const chunk = decoder.decode(value, { stream: true });
          const items = chunk.split('\n').filter(item => item.trim());
          
          items.forEach(item => {
            try {
              const parsedItem = JSON.parse(item);
              setMessages(prev => [...prev, parsedItem]);
            } catch (e) {
              console.error('Failed to parse item:', item);
            }
          });
        }
      } catch (error) {
        if (error.name !== 'AbortError') {
          console.error('Stream error:', error);
        }
      }
    }
    
    fetchStream();
    
    return () => {
      abortController.abort();
    };
  }, []);
  
  return (
    <div>
      <h2>Stream Data</h2>
      <ul>
        {messages.map((message, index) => (
          <li key={index}>{JSON.stringify(message)}</li>
        ))}
      </ul>
    </div>
  );
}























import { useState, useEffect } from 'react';

function SSEComponent() {
  const [messages, setMessages] = useState([]);
  
  useEffect(() => {
    const eventSource = new EventSource('https://your-api/sse-endpoint');
    
    eventSource.onmessage = (event) => {
      const newMessage = JSON.parse(event.data);
      setMessages(prev => [...prev, newMessage]);
    };
    
    eventSource.onerror = (error) => {
      console.error('SSE error:', error);
      eventSource.close();
    };
    
    return () => {
      eventSource.close();
    };
  }, []);
  
  return (
    <div>
      <h2>SSE Stream</h2>
      <ul>
        {messages.map((message, index) => (
          <li key={index}>{JSON.stringify(message)}</li>
        ))}
      </ul>
    </div>
  );
}





*/
