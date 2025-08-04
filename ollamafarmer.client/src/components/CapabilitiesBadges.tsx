import type { components } from "../api/schema";


export interface CapabilitiesBadgesProps {
    capabilities?: components["schemas"]["ModelCapabilities"];
}

function CapabilitiesBadges(props: CapabilitiesBadgesProps) {

    return <div>
        {props.capabilities?.supportsCompletion &&
            (<span className="badge ms-2 bg-success">Completion</span>)}
        {props.capabilities?.supportsTools &&
            (<span className="badge ms-2 bg-info">Tools</span>)}
        {props.capabilities?.supportsVision &&
            (<span className="badge ms-2 bg-light text-dark">Vision</span>)}
        {props.capabilities?.supportsThinking &&
            (<span className="badge ms-2 bg-primary">Thinking</span>)}
    </div>
}

export default CapabilitiesBadges;
