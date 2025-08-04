import { useOutlet, useLocation } from 'react-router';
import { useRef } from 'react';
import './App.css';
import { CSSTransition, SwitchTransition } from 'react-transition-group';
import { Navi } from './Navi.tsx';
import { ToastContainer, Bounce } from 'react-toastify';

import { NotificationHubProvider } from './context/NotificationHubProvider.tsx';
import { DialogContextProvider } from './context/DialogContext.tsx';

function App() {
    const location = useLocation();
    const currentOutlet = useOutlet();
    const nodeRef = useRef(null);

    return (
        <div className="">
            <DialogContextProvider>
                <NotificationHubProvider>
                    <Navi />
                    <div className="mt-5">
                        <SwitchTransition mode="out-in">
                            <CSSTransition
                                key={location.pathname}
                                nodeRef={nodeRef}
                                timeout={200}
                                classNames="page"
                                unmountOnExit
                            >
                                {() => (
                                    <div ref={nodeRef} className="page">
                                        {currentOutlet}
                                    </div>
                                )}
                            </CSSTransition>
                        </SwitchTransition>
                    </div>
                    <ToastContainer
                        position="bottom-right"
                        autoClose={3000}
                        hideProgressBar={true}
                        pauseOnFocusLoss={true}
                        pauseOnHover
                        theme="dark"
                        limit={4}
                        transition={Bounce}
                    />
                </NotificationHubProvider>
            </DialogContextProvider>
        </div>
    );
}

export default App;
