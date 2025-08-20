// import { useLocation, NavLink, Routes, Route } from "react-router";
// import { TransitionGroup, CSSTransition } from "react-transition-group";
// import Chat from "./pages/Chat";
// import Home from "./pages/Home";
// import ERR404 from "./pages/system/ERR404";
// import Weather from "./pages/Weather";
// import ReactDOM from "react-dom";

import { createRef } from "react";
import { createBrowserRouter } from "react-router";
import App from "./App";
import Chat from "./pages/chat/Chat";
import Home from "./pages/Home";
import ERR404 from "./pages/system/ERR404";
import ChatList from "./pages/chat/ChatList";
import Test from "./pages/Test";
import ModelList from "./pages/models/ModelList";
import SignalRPanel from "./pages/SignalRPanel";
import ERR from "./pages/system/ERR";
import FileExplorer from "./pages/files/FileExplorer";
import ToolsList from "./pages/tools/ToolsList";
import SysPage from "./pages/system/SysPage";
import McpServerList from "./pages/tools/McpServerList";
import ChatServerList from "./pages/chat/ChatServerList";
import ToolBagsPage from "./pages/tools/ToolBagsPage";
import ConsultantList from "./pages/consultants/ConsultantList";


export const routes = [
{ path: '/', name: 'Home', element: <Home />, nodeRef: createRef<HTMLDivElement>() },
{ path: '/signalr', name: 'SignalR', element: <SignalRPanel />, nodeRef: createRef<HTMLDivElement>() },
{
    path: '/chats',
    name: 'Chats',
    element: <ChatList />,
    nodeRef: createRef<HTMLDivElement>(),
},
{
    path: '/chat',
    name: 'Chat',
    element: <Chat />,
    nodeRef: createRef<HTMLDivElement>(),
},
{
    path: '/chat/:id',
    name: 'Chat',
    element: <Chat />,
    nodeRef: createRef<HTMLDivElement>(), hide: true
},
{
    path: '/test',
    name: 'Test',
    element: <Test />,
    nodeRef: createRef<HTMLDivElement>(), hide: true
},
{
    path: '/models',
    name: 'Models',
    element: <ModelList />,
    nodeRef: createRef<HTMLDivElement>(), hide: false
},
{
    path: '/consultants',
    name: 'Consultants',
    element: <ConsultantList />,
    nodeRef: createRef<HTMLDivElement>(), hide: false
},
{
    path: '/files',
    name: 'Files',
    element: <FileExplorer />,
    nodeRef: createRef<HTMLDivElement>(), hide: false
},
{
    path: '/servers',
    name: 'Servers',
    element: <ChatServerList />,
    nodeRef: createRef<HTMLDivElement>(), hide: false
},
{
    path: '/mcp',
    name: 'MCP Servers',
    element: <McpServerList />,
    nodeRef: createRef<HTMLDivElement>(), hide: false
},
{
    path: '/tools',
    name: 'Tools',
    element: <ToolsList />,
    nodeRef: createRef<HTMLDivElement>(), hide: false
},
{
    path: '/toolbags',
    name: 'ToolBags',
    element: <ToolBagsPage />,
    nodeRef: createRef<HTMLDivElement>(), hide: false
},
{
    path: '/system',
    name: 'System',
    element: <SysPage />,
    nodeRef: createRef<HTMLDivElement>(), hide: false
},


{ path: '/error', name: 'ERR', element: <ERR />, nodeRef: createRef<HTMLDivElement>(), hide: true },
{ path: '*', name: '404', element: <ERR404 />, nodeRef: createRef<HTMLDivElement>(), hide: true },
];

export const router = createBrowserRouter([
{
    path: '/',
    element: <App />,
    children: routes.map((route) => ({
        index: route.path === '/',
        path: route.path === '/' ? undefined : route.path,
        element: route.element,
    })),
},
]);

// function AppRouter() {
//     const location = useLocation();

//     return (
//         <div className="">
//                 {/* <Header navItems={navItems}></Header> */}
//                
//                 <div className="">
//                     <nav className="navbar navbar-expand-lg bg-dark py-1 px-4 sticky-lg-top">
//                         <NavLink to="/" viewTransition className="navbar-brand" end>O</NavLink>
//                         <div className="vr me-4"></div>
//                         <NavLink to="/weather" viewTransition className="nav-link me-4" end>Weather</NavLink>
//                         <NavLink to="/chat" viewTransition className="nav-link me-4" end>Chat</NavLink>
//                     </nav>
//                     <div className="">
//                         <div className="container-fluid m-0 p-0">
//                             <TransitionGroup>
//                                 <CSSTransition key={location.key} classNames="fade" timeout={300}>
//                                     <Routes>
//                                         <Route path="/" element={<Home />} ></Route>
//                                         <Route path="/home" element={<Home />}></Route>
//                                         <Route path="/weather" element={<Weather />}></Route>
//                                         <Route path="/chat" element={<Chat />}>
//                                             <Route path="/chat/:id" element={<Chat />} />
//                                         </Route>
//                                         <Route path="*" element={<ERR404 />} />
//                                     </Routes>
//                                 </CSSTransition>
//                             </TransitionGroup>
//                         </div>
//                     </div>
//                 </div>
//                 {/* <Footer></Footer> */}
//         </div>
//     );
// }
// export default AppRouter;
