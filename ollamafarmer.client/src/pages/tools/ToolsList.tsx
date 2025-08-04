import { faArrowDown, faArrowsRotate, faWrench } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { $queryClient } from "../../api/api";
import type { McpServer, McpTool } from "../../api/apiTypes";

// Tool selection types
export interface ToolSelectionState {
    selectedToolIds: string[];
    setSelectedToolIds: (ids: string[]) => void;
}

export interface ServerToolsGroup {
    server: McpServer;
    tools: McpTool[];
    selectedToolIds: Set<string>;
}

  
function ToolsList() {

    const pageSize = 10;

    const { data, error, fetchNextPage, hasNextPage, isFetching, isLoading, refetch } = $queryClient.useInfiniteQuery("get", "/api/Tools/paged", { params: { query: { pageSize: pageSize } } }, {
        getNextPageParam: (lastPage: { cursor: number; filteredCount: number; }) =>
            pageSize * ((lastPage.cursor ?? 0) + 1) >= (lastPage.filteredCount ?? 0)
                ? undefined : (lastPage.cursor ?? 0) + 1,
        getPreviousPageParam: (firstPage: { cursor: number; }) => firstPage.cursor ?? 0,
        initialPageParam: 0,
    });

    const placeholderRow = <tr>
        {[...Array(2)].map((_, i) =>
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
                        <th className="">Name</th>
                        <th className="col">Description</th>
                    </tr>
                </thead>
                <tbody >
                    {isLoading && placeholderRow}
                    {data.pages.map(page => {
                        return page.data?.map((toolDetails) => {
                            return (
                                <tr key={toolDetails.name}>
                                    <td className="text-nowrap">
                                        <div className="d-flex flex-row align-items-center">
                                            <FontAwesomeIcon icon={faWrench} className="me-2" />
                                            <span className="text-truncate">{toolDetails.name}</span>
                                        </div>
                                    </td>
                                    <td className="text-truncate">{toolDetails.description}</td>
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
                        <td colSpan={2} className="text-center text-muted">No tools found. {data.pages[0].filteredCount}</td>
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



    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">Model Tools</h1>
                <div className="d-flex flex-row p-1">
                    <button type="button" disabled={isFetching} className="btn btn-primary me-3" aria-label="New chat" onClick={() => refetch()}>
                        <FontAwesomeIcon icon={faArrowsRotate} className={"me-2 " + (isFetching ? "refresh-rotate" : "")} />Refresh</button>
                    {/* <button type="button" className="btn btn-primary ms-3" aria-label="New chat" onClick={() => {}}>
                        <FontAwesomeIcon icon={faPlus} className="me-2 " />New</button> */}
                </div>
            </div>
            <div className="container-fluid">
                {errorFragment
                    //|| isLoading && <LoadingPanel />     
                    || noDataFragment
                    || tableDataFragment}
            </div>
           
           
        </div>
    );
}

export default ToolsList;
