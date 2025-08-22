import { useMemo, useState } from "react";
import { $queryClient } from "../../api/api";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowsRotate, faPlus, faTrash, faEdit } from "@fortawesome/free-solid-svg-icons";
import ChatServerSelector from "../../components/ChatServerSelector";
import LoadingPanel from "../../components/LoadingPanel";
import { toast } from "react-toastify";
import { useDialogContext } from "../../hooks/useDialogContext";

interface ConsultantDto {
  id: string;
  consultantId: string; // internal identifier, not displayed
  name: string;
  topic: string;
  chatModelId: string;
  chatServerId: string;
  systemMessage?: string | null;
  expertiseLevel: "Beginner" | "Intermediate" | "Advanced" | "Expert";
  temperature: number;
  topP: number;
  frequencyPenalty: number;
  presencePenalty: number;
}

interface ChatModelDto { id: string; name?: string | null; model?: string | null; }

function ConsultantList() {
  const [selectedServerId, setSelectedServerId] = useState<string>("");
  const [showCreate, setShowCreate] = useState(false);
  const [editing, setEditing] = useState<ConsultantDto | null>(null);
  const dialogs = useDialogContext();

  // OpenAPI client doesn't have this path typed; suppress explicit-any for path cast
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { data: consultants, refetch, isFetching, isLoading, error } = $queryClient.useQuery("get", "/api/Consultants" as any, {
    params: { query: { serverId: selectedServerId || undefined } },
    enabled: !!selectedServerId,
  });

  const { data: models } = $queryClient.useQuery("get", "/api/ChatModel/all", {
    params: { query: { serverId: selectedServerId || undefined } },
    enabled: !!selectedServerId,
  });

  const modelMap = useMemo(() => {
    const dict = new Map<string, ChatModelDto>();
    const arr = ((models as unknown as ChatModelDto[]) || []);
    arr.forEach((m) => { if (m?.id) dict.set(m.id, m); });
    return dict;
  }, [models]);

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { mutateAsync: createAsync } = $queryClient.useMutation("post", "/api/Consultants" as any, {});
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { mutateAsync: updateAsync } = $queryClient.useMutation("put", "/api/Consultants/{id}" as any, {});
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const { mutateAsync: deleteAsync } = $queryClient.useMutation("delete", "/api/Consultants/{id}" as any, {});

  const handleSave = async (payload: Partial<ConsultantDto>) => {
    if (!selectedServerId) return;
    try {
      if (editing) {
  await updateAsync({ params: { path: { id: editing.id } }, body: payload as unknown as Record<string, unknown> });
        toast.success("Consultant updated");
      } else {
  await createAsync({ body: payload as unknown as Record<string, unknown> });
        toast.success("Consultant created");
      }
      setEditing(null);
      setShowCreate(false);
      refetch();
    } catch (e: unknown) {
        const msg = (e as { message?: string })?.message ?? String(e);
        toast.error(msg);
    }
  };

  const handleDelete = async (c: ConsultantDto) => {
    dialogs.showDangerConfirmDialog(
      "Delete Consultant",
      `Delete consultant "${c.name}"? This action cannot be undone.`,
      async () => {
        await deleteAsync({ params: { path: { id: c.id } } });
        toast.success("Consultant deleted");
        refetch();
      }
    );
  };

  return (
    <div className="container-fluid pt-4">
      <div className="container-fluid">
        <h1 className="display-5">Consultants</h1>
        <div className="d-flex flex-row p-1 mb-3 align-items-end">
          <div className="me-3">
            <ChatServerSelector selectedServerId={selectedServerId} onServerChange={setSelectedServerId} style={{ minWidth: "200px" }} />
          </div>
          <button className="btn btn-primary me-2" disabled={!selectedServerId} onClick={() => { setEditing(null); setShowCreate(true); }}>
            <FontAwesomeIcon icon={faPlus} className="me-2" />New
          </button>
          <button className="btn btn-secondary" disabled={!selectedServerId || isFetching} onClick={() => refetch()}>
            <FontAwesomeIcon icon={faArrowsRotate} className={"me-2 " + (isFetching ? "refresh-rotate" : "")} />Refresh
          </button>
        </div>
      </div>
      <div className="container-fluid">
        {!selectedServerId && <div className="m-2 p-2"><p>Select a chat server to view and manage consultants.</p></div>}
        {selectedServerId && (error ? (
          <div className="alert alert-danger">Failed to load consultants</div>
        ) : isLoading ? (
          <LoadingPanel />
        ) : (
          <div className="p-1">
            <table className="table table-striped table-hover">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Topic</th>
                  <th>Model</th>
                  <th>Expertise</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {((consultants as unknown as ConsultantDto[]) || []).map((c: ConsultantDto) => {
                  const m = modelMap.get(c.chatModelId);
                  return (
                    <tr key={c.id}>
                      <td>{c.name}</td>
                      <td>{c.topic}</td>
                      <td>{m?.name || m?.model || c.chatModelId}</td>
                      <td>{c.expertiseLevel}</td>
                      <td className="text-nowrap">
                        <button className="btn btn-sm btn-secondary me-2" onClick={() => { setEditing(c); setShowCreate(true); }}>
                          <FontAwesomeIcon icon={faEdit} className="me-2" />Edit
                        </button>
                        <button className="btn btn-sm btn-danger" onClick={() => handleDelete(c)}>
                          <FontAwesomeIcon icon={faTrash} className="me-2" />Delete
                        </button>
                      </td>
                    </tr>
                  );
                })}
                {((consultants as unknown as ConsultantDto[]) || []).length === 0 && !isFetching && (
                  <tr><td colSpan={5} className="text-center text-muted">No consultants found</td></tr>
                )}
              </tbody>
            </table>
          </div>
        ))}
      </div>

      {showCreate && (
        <ConsultantEditor
          onClose={() => { setShowCreate(false); setEditing(null); }}
          onSave={handleSave}
          consultant={editing}
          models={((models as unknown as ChatModelDto[]) || [])}
        />
      )}
    </div>
  );
}

function ConsultantEditor({ onClose, onSave, consultant, models }: { onClose: () => void; onSave: (c: Partial<ConsultantDto>) => void; consultant: ConsultantDto | null; models: ChatModelDto[]; }) {
  const [name, setName] = useState(consultant?.name || "");
  const [topic, setTopic] = useState(consultant?.topic || "General");
  const [chatModelId, setChatModelId] = useState(consultant?.chatModelId || (models[0]?.id || ""));
  const [systemMessage, setSystemMessage] = useState(consultant?.systemMessage || "");
  const [expertiseLevel, setExpertiseLevel] = useState<ConsultantDto["expertiseLevel"]>(consultant?.expertiseLevel || "Beginner");
  const [temperature, setTemperature] = useState(consultant?.temperature ?? 0.7);
  const [topP, setTopP] = useState(consultant?.topP ?? 1.0);
  const [frequencyPenalty, setFrequencyPenalty] = useState(consultant?.frequencyPenalty ?? 0.0);
  const [presencePenalty, setPresencePenalty] = useState(consultant?.presencePenalty ?? 0.0);

  const isEdit = !!consultant;

  const submit = () => {
    const payload = {
      name,
      topic,
      chatModelId,
      expertiseLevel,
      systemMessage,
      temperature,
      topP,
      frequencyPenalty,
      presencePenalty,
    };
    onSave(payload);
  };

  return (
    <div className="modal d-block" tabIndex={-1} role="dialog">
      <div className="modal-dialog modal-xl modal-fullscreen-lg-down modal-dialog-scrollable modal-dialog-centered" role="document">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">{isEdit ? "Edit Consultant" : "Create Consultant"}</h5>
            <button type="button" className="btn-close" aria-label="Close" onClick={onClose}></button>
          </div>
          <div className="modal-body">
            <div className="row g-3">
              <div className="col-md-4">
                <label className="form-label">Name</label>
                <input className="form-control" value={name} onChange={(e) => setName(e.target.value)} />
              </div>
              <div className="col-md-4">
                <label className="form-label">Topic</label>
                <input className="form-control" value={topic} onChange={(e) => setTopic(e.target.value)} />
              </div>
              <div className="col-md-4">
                <label className="form-label">Model</label>
                <select className="form-select" value={chatModelId} onChange={(e) => setChatModelId(e.target.value)}>
                  {models
                    .slice()
                    .sort((a,b) => (a.name || a.model || "").localeCompare(b.name || b.model || ""))
                    .map((m) => (
                      <option key={m.id} value={m.id}>{m.name || m.model}</option>
                    ))}
                </select>
              </div>
              <div className="col-12">
                <label className="form-label">System Message</label>
                <textarea
                  className="form-control"
                  rows={8}
                  style={{ minHeight: 180 }}
                  value={systemMessage ?? ""}
                  onChange={(e) => setSystemMessage(e.target.value)}
                />
              </div>
              <div className="col-md-3">
                <label className="form-label">Expertise</label>
                <select className="form-select" value={expertiseLevel} onChange={(e) => setExpertiseLevel(e.target.value as ConsultantDto["expertiseLevel"])}>
                  {['Beginner','Intermediate','Advanced','Expert'].map(x => <option key={x} value={x}>{x}</option>)}
                </select>
              </div>
              <div className="col-md-2">
                <label className="form-label">Temperature</label>
                <input type="number" step="0.1" className="form-control" value={temperature} onChange={(e) => setTemperature(parseFloat(e.target.value))} />
              </div>
              <div className="col-md-2">
                <label className="form-label">TopP</label>
                <input type="number" step="0.1" className="form-control" value={topP} onChange={(e) => setTopP(parseFloat(e.target.value))} />
              </div>
              <div className="col-md-2">
                <label className="form-label">Freq Penalty</label>
                <input type="number" step="0.1" className="form-control" value={frequencyPenalty} onChange={(e) => setFrequencyPenalty(parseFloat(e.target.value))} />
              </div>
              <div className="col-md-2">
                <label className="form-label">Presence Penalty</label>
                <input type="number" step="0.1" className="form-control" value={presencePenalty} onChange={(e) => setPresencePenalty(parseFloat(e.target.value))} />
              </div>
            </div>
          </div>
          <div className="modal-footer">
            <button className="btn btn-primary" onClick={submit}><FontAwesomeIcon icon={faPlus} className="me-2" />Save</button>
            <button className="btn btn-secondary" onClick={onClose}>Cancel</button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ConsultantList;
