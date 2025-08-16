import { useState } from "react";
import { ApiBaseUrl } from "../../api/api";
import { Input } from "reactstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowsRotate, faEdit, faPlus, faTrash, faWrench, faFolder } from "@fortawesome/free-solid-svg-icons";
import { ToolSelectionModal } from "../../components/ToolSelectionModal";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialogContext } from "../../hooks/useDialogContext";

type ToolBagDto = {
    id: string;
    name: string;
    description?: string | null;
    createdAt?: string;
    updatedAt?: string;
    toolIds: string[];
};

async function fetchJson<T>(path: string, init?: RequestInit): Promise<T> {
    const resp = await fetch(`${ApiBaseUrl()}${path}`, {
        ...init,
        headers: {
            'Content-Type': 'application/json',
            ...(init?.headers || {})
        },
    });
    if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
    return resp.json() as Promise<T>;
}

function ToolBagsPage() {
    const dialogs = useDialogContext();
    const queryClient = useQueryClient();

    const { data: bags, refetch, isFetching, isLoading, error } = useQuery<ToolBagDto[]>({
        queryKey: ["toolbags"],
        queryFn: () => fetchJson<ToolBagDto[]>("/api/ToolBags"),
        refetchOnWindowFocus: false,
    });

    const [showModal, setShowModal] = useState(false);
    const [assignToolsBag, setAssignToolsBag] = useState<ToolBagDto | null>(null);
    const [newBagName, setNewBagName] = useState("");
    const [newBagDesc, setNewBagDesc] = useState("");

    const placeholderRow = (
        <tr>
            {[...Array(3)].map((_, i) => (
                <td key={i} className="placeholder-glow">
                    <span className="placeholder col-6 m-2 bg-primary"></span>
                </td>
            ))}
        </tr>
    );

    const noDataFragment = (!bags || bags.length === 0) && !isFetching && (
        <div className="" aria-hidden="true">
            <h5 className="card-title">No data</h5>
            <p className="card-text">No ToolBags available.</p>
        </div>
    );

    const errorFragment = error && (
        <div className="d-inline-flex card bg-danger-subtle" aria-hidden="true">
            <div className="card-body">
                <h5 className="card-title">Error</h5>
                <p className="card-text">Failed to retrieve ToolBags, response: <code>{String((error as Error).message)}</code></p>
            </div>
        </div>
    );

    const handleCreateBag = async () => {
        if (!newBagName.trim()) return dialogs.showWarningDialog("Missing name", "Please provide a name for the ToolBag.");
        const created = await fetchJson<ToolBagDto>(`/api/ToolBags`, {
            method: 'POST',
            body: JSON.stringify({ name: newBagName, description: newBagDesc })
        });
        setNewBagName("");
        setNewBagDesc("");
        // Optimistically update cache and then invalidate
        queryClient.setQueryData<ToolBagDto[] | undefined>(["toolbags"], (prev) => {
            const list = prev ?? [];
            // Avoid dupes by id
            if (list.find(b => b.id === created.id)) return list;
            return [created, ...list];
        });
        await queryClient.invalidateQueries({ queryKey: ["toolbags"] });
        dialogs.showSuccessDialog("Created", "ToolBag has been created.");
    };

    const handleDeleteBag = async (id: string, name: string) => {
        dialogs.showDangerConfirmDialog(
            "Delete ToolBag",
            `Are you sure you want to delete ToolBag "${name}"? This action cannot be undone.`,
            async () => {
                await fetch(`${ApiBaseUrl()}/api/ToolBags/${id}`, { method: 'DELETE' });
                // Update cache immediately
                queryClient.setQueryData<ToolBagDto[] | undefined>(["toolbags"], (prev) => (prev ?? []).filter(b => b.id !== id));
                await queryClient.invalidateQueries({ queryKey: ["toolbags"] });
                dialogs.showSuccessDialog("Deleted", `ToolBag "${name}" deleted.`);
            }
        );
    };

    const handleRenameBag = async (bag: ToolBagDto) => {
        // Ask for new name first
        dialogs.showTextInputDialog(
            "Rename ToolBag",
            `Enter a new name for "${bag.name}"`,
            bag.name,
            async (nameValue) => {
                // Then ask for description
                dialogs.showTextInputDialog(
                    "Description",
                    "Enter an optional description",
                    bag.description ?? "",
                    async (descValue) => {
                        const updated = await fetchJson<ToolBagDto>(`/api/ToolBags/${bag.id}`, {
                            method: 'PUT',
                            body: JSON.stringify({ name: nameValue, description: descValue })
                        });
                        // Update cache immediately
                        queryClient.setQueryData<ToolBagDto[] | undefined>(["toolbags"], (prev) => (prev ?? []).map(b => b.id === updated.id ? updated : b));
                        await queryClient.invalidateQueries({ queryKey: ["toolbags"] });
                        dialogs.showSuccessDialog("Updated", `ToolBag "${nameValue}" saved.`);
                    }
                );
            }
        );
    };

    const handleAssignTools = (bag: ToolBagDto) => {
        setAssignToolsBag(bag);
        setShowModal(true);
    };

    const handleConfirmAssignTools = async (toolIds: string[]) => {
        if (!assignToolsBag) return;
        await fetchJson<void>(`/api/ToolBags/${assignToolsBag.id}/tools`, {
            method: 'PUT',
            body: JSON.stringify({ toolIds })
        });
        setShowModal(false);
        // Update cache for count immediately
        queryClient.setQueryData<ToolBagDto[] | undefined>(["toolbags"], (prev) => (prev ?? []).map(b => b.id === assignToolsBag.id ? { ...b, toolIds } : b));
        setAssignToolsBag(null);
        await queryClient.invalidateQueries({ queryKey: ["toolbags"] });
        dialogs.showSuccessDialog("Updated", "Tool assignments saved.");
    };

    const tableFragment = bags && (
        <div className="p-1">
            <table className="table table-striped table-hover" aria-labelledby="tableLabel">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Description</th>
                        <th>Tools</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {isLoading && placeholderRow}
                    {bags?.map((bag) => (
                        <tr key={bag.id}>
                            <td className="text-nowrap"><FontAwesomeIcon icon={faFolder} className="me-2" />{bag.name}</td>
                            <td className="text-truncate">{bag.description || "No description"}</td>
                            <td className="text-center">
                                <span className={"badge " + ((bag.toolIds?.length || 0) > 0 ? "bg-primary" : "bg-warning")}>{bag.toolIds?.length ?? 0}</span>
                            </td>
                            <td className="text-nowrap">
                                <button className="btn btn-sm btn-primary me-2" onClick={() => handleRenameBag(bag)} title="Rename">
                                    <FontAwesomeIcon icon={faEdit} className="me-2" />
                                    Edit
                                </button>
                                <button className="btn btn-sm btn-secondary me-2" onClick={() => handleAssignTools(bag)} title="Assign Tools">
                                    <FontAwesomeIcon icon={faWrench} className="me-2" />
                                    Assign Tools
                                </button>
                                <button className="btn btn-sm btn-danger" onClick={() => handleDeleteBag(bag.id, bag.name)} title="Delete">
                                    <FontAwesomeIcon icon={faTrash} className="me-2" />
                                    Delete
                                </button>
                            </td>
                        </tr>
                    ))}
                    {isFetching && placeholderRow}
                    {bags?.length === 0 && !isFetching && (
                        <tr>
                            <td colSpan={4} className="text-center text-muted">No ToolBags found.</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );

    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">ToolBags</h1>
                <div className="d-flex flex-row p-1 align-items-end">
                    <button type="button" disabled={isFetching} className="btn btn-primary me-3" aria-label="Refresh" onClick={() => refetch()}>
                        <FontAwesomeIcon icon={faArrowsRotate} className={"me-2 " + (isFetching ? "refresh-rotate" : "")} />
                        Refresh
                    </button>
                    <div className="d-flex flex-row align-items-end">
                        <div className="me-2">
                            <label className="form-label">Name</label>
                            <Input value={newBagName} onChange={(e) => setNewBagName(e.target.value)} placeholder="New ToolBag name" />
                        </div>
                        <div className="me-2">
                            <label className="form-label">Description</label>
                            <Input value={newBagDesc} onChange={(e) => setNewBagDesc(e.target.value)} placeholder="Optional description" />
                        </div>
                        <button type="button" className="btn btn-primary" onClick={handleCreateBag}>
                            <FontAwesomeIcon icon={faPlus} className="me-2" />
                            Create ToolBag
                        </button>
                    </div>
                </div>
            </div>
            <div className="container-fluid">
                {errorFragment || (!bags && !isFetching && noDataFragment) || tableFragment}
            </div>

            {/* Use ToolSelectionModal to choose tools for a bag; ignore bag selection in this context */}
            <ToolSelectionModal
                isOpen={showModal}
                onClose={() => { setShowModal(false); setAssignToolsBag(null); }}
                onConfirm={(toolIds /*, bagIds*/ ) => handleConfirmAssignTools(toolIds)}
                initialSelectedToolIds={assignToolsBag?.toolIds ?? []}
                initialSelectedBagIds={[]}
            />
        </div>
    );
}

export default ToolBagsPage;
