import { $queryClient } from "../../api/api";

function SysQueue() {
    const { data: queueDescription, error: queueDescriptionError, isLoading: queueDescriptionLoading, refetch: refetchQueueDescription } = $queryClient.useQuery("get", "/api/SysQueue/describe", {});
    const { data: queueItems, error: queueError, isLoading: queueLoading, refetch: refetchQueueItems } = $queryClient.useQuery("get", "/api/SysQueue/items", {});
    const { mutate: clearQueue, isPending: isClearing } = $queryClient.useMutation("post", "/api/SysQueue/clear", {
        onSuccess: () => {
            refetchQueueItems();
            refetchQueueDescription();
        },
    });



    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <div>
                    <h4 className="">Queue Items</h4>
                    <p>
                        {queueDescriptionLoading ? "Loading queue description..." : ""}
                        {queueDescriptionError ? `Error: ${"message" in queueDescriptionError ? (queueDescriptionError as { message: string }).message : String(queueDescriptionError)}` : ""}
                        {queueDescription ? `There are ${queueDescription.queuedCount} items in the queue.` : "Loading..."}
                    </p>
                    <table className="table table-striped">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Description</th>
                                <th>Created At</th>
                                <th>Status</th>
                                <th className="text-end">
                                    <button className="btn btn-danger" onClick={() => clearQueue(undefined)} disabled={isClearing}>
                                        {isClearing ? "Clearing..." : "Clear Queue"}
                                    </button>
                                    <button className="btn btn-secondary ms-2" onClick={() => {
                                        refetchQueueItems();
                                        refetchQueueDescription();
                                    }}>
                                        Refresh
                                    </button>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            {queueLoading && (
                                <tr>
                                    <td colSpan={5} className="text-center">Loadinssssg...</td>
                                </tr>
                            )}
                            {queueError && (
                                <tr>
                                    <td colSpan={5} className="text-danger">
                                        Error: {"message" in queueError ? (queueError as { message: string }).message : String(queueError)}
                                    </td>
                                </tr>
                            )}
                            {!queueLoading && queueItems && queueItems.length > 0 ? (
                                queueItems.map((item, index) => (
                                    <tr key={index}>
                                        <td>{item.name}</td>
                                        <td>{item.description}</td>
                                        <td className="text-end">{item.createdAt}</td>
                                        <td className="text-end">
                                            {item.isRunning ? (
                                                <span className="badge bg-warning">Processing</span>
                                            ) : item.isCompleted ? (
                                                <span className="badge bg-success">Completed</span>
                                            ) : (
                                                <span className="badge bg-secondary">Pending</span>
                                            )}
                                        </td>
                                        <td className="text-end">
                                            
                                        </td>
                                    </tr>
                                ))
                            ) : (
                                <tr>
                                    <td colSpan={5} className="text-center text-muted">No items in the queue.</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );

}

export default SysQueue;
