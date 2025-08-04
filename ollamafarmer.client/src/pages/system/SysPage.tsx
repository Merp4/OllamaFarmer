// import { useEffect } from 'react';

import SysQueue from "./SysQueue";


// 404 page

function SysPage() {

    // useEffect(() => {
    // }, []);


    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">System</h1>
                <p>
                    This page provides access to system-level features and information.
                    It includes a background processing queue that displays the current state of the queue,
                    including the number of items, their status, and allows you to clear the queue.
                </p>
            </div>
            <div className="container-fluid">
                <SysQueue />
            </div>
        </div>
    );

}

export default SysPage;
