//import { $api } from "../api/api";


function Test() {


    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">Test</h1>
                <p className="p-1">This is the Test component.</p>
            </div>
            <div className="container-fluid">
                <div className="container-fluid">
                    <h2 className="display-5">Buttons</h2>
                    <p className="p-1">
                        <button type="button" className="m-2 btn btn-primary">Primary</button>
                        <button type="button" className="m-2 btn btn-secondary">Secondary</button>
                        <button type="button" className="m-2 btn btn-success">Success</button>
                        <button type="button" className="m-2 btn btn-danger">Danger</button>
                        <button type="button" className="m-2 btn btn-warning">Warning</button>
                        <button type="button" className="m-2 btn btn-info">Info</button>
                        <button type="button" className="m-2 btn btn-light">Light</button>
                        <button type="button" className="m-2 btn btn-dark">Dark</button>
                    </p>
                    <p className="p-1">
                        <button type="button" className="m-2 btn btn-primary btn-sm ">Primary</button>
                        <button type="button" className="m-2 btn btn-secondary btn-sm ">Secondary</button>
                        <button type="button" className="m-2 btn btn-success btn-sm ">Success</button>
                        <button type="button" className="m-2 btn btn-danger btn-sm ">Danger</button>
                        <button type="button" className="m-2 btn btn-warning btn-sm ">Warning</button>
                        <button type="button" className="m-2 btn btn-info btn-sm ">Info</button>
                        <button type="button" className="m-2 btn btn-light btn-sm ">Light</button>
                        <button type="button" className="m-2 btn btn-dark btn-sm ">Dark</button>
                    </p>
                </div>
                <div className="container-fluid">
                    <h2 className="display-5">Cards</h2>
                    <div className="p-1">
                        <div className="card col-md-3 d-inline-block m-2">
                            <div className="card-body">
                                <h5 className="card-title">Card default</h5>
                                <h6 className="card-subtitle mb-2">Card default</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                        <div className="card col-md-3 d-inline-block m-2 text-bg-primary">
                            <div className="card-body">
                                <h5 className="card-title">Card primary</h5>
                                <h6 className="card-subtitle mb-2 ">Card primary</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                        <div className="card col-md-3 d-inline-block m-2 text-bg-secondary">
                            <div className="card-body">
                                <h5 className="card-title">Card secondary</h5>
                                <h6 className="card-subtitle mb-2 ">Card secondary</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                        <div className="card col-md-3 d-inline-block m-2 text-bg-success">
                            <div className="card-body">
                                <h5 className="card-title">Card success</h5>
                                <h6 className="card-subtitle mb-2 ">Card success</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                        <div className="card col-md-3 d-inline-block m-2 text-bg-warning">
                            <div className="card-body">
                                <h5 className="card-title">Card warning</h5>
                                <h6 className="card-subtitle mb-2 ">Card warning</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                        <div className="card col-md-3 d-inline-block m-2 text-bg-danger">
                            <div className="card-body">
                                <h5 className="card-title">Card danger</h5>
                                <h6 className="card-subtitle mb-2 ">Card danger</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                        <div className="card col-md-3 d-inline-block m-2 text-bg-info">
                            <div className="card-body">
                                <h5 className="card-title">Card info</h5>
                                <h6 className="card-subtitle mb-2 ">Card info</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                        <div className="card col-md-3 d-inline-block m-2 text-bg-light">
                            <div className="card-body">
                                <h5 className="card-title">Card light</h5>
                                <h6 className="card-subtitle mb-2 ">Card light</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                        <div className="card col-md-3 d-inline-block m-2 text-bg-dark">
                            <div className="card-body">
                                <h5 className="card-title">Card dark</h5>
                                <h6 className="card-subtitle mb-2 ">Card dark</h6>
                                <p className="card-text">Some quick example text to build on the card title and make up the bulk of the card's content.</p>
                                <a href="#" className="card-link">Card link</a>
                                <a href="#" className="card-link">Another link</a>
                            </div>
                        </div>
                    </div>

                </div>
            </div>
        </div>
    );
}

export default Test;
