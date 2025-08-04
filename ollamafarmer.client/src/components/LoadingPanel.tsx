
import { Progress } from 'reactstrap';

export interface LoadingPanelProps {
    text?: string;
    progress?: number;
}


function LoadingPanel(props: LoadingPanelProps) {

    return (
        props && (<div className="mx-auto mt-5 col text-center" aria-hidden="true">
            <div className="d-inline-block text-start px-4 py-4">
                <div className="w-100">
                    <h5 className="mx-5">
                        {props.text ?? "Loading..."}
                    </h5>
                    <Progress animated value={props.progress ?? 100} ></Progress>
                </div>
            </div>
        </div>)
    );
}

export default LoadingPanel;

