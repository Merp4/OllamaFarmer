import { NavLink } from "react-router";
import { OpenApiSpecUrl } from "./api/api";
import { useState } from "react";
import { Dropdown, DropdownToggle, DropdownMenu, DropdownItem } from "reactstrap";

export const Navi = () => {
    const [dropdownOpen, setDropdownOpen] = useState(false);
    const toggle = () => setDropdownOpen((prevState) => !prevState);

    return (
        <nav className="navbar navbar-expand-md bg-dark text-light shadow-sm fixed-top">
            {/* <NavLink to="/" className="navbar-brand text-light" end>Farm</NavLink>
            <div className="vr me-4"></div> */}
            {/* <NavLink to="/chat" viewTransition className="nav-link me-4" end>New Chat</NavLink> */}
            <NavLink to="/chats" viewTransition className="nav-link me-4" end>Chats</NavLink>
            <NavLink to="/models" viewTransition className="nav-link me-4" end>Models</NavLink>
            <NavLink to="/files" viewTransition className="nav-link me-4" end>Files</NavLink>
            {/* <NavLink to="/tools" viewTransition className="nav-link me-4" end>Tools</NavLink> */}
            <NavLink to="/servers" viewTransition className="nav-link me-4" end>Servers</NavLink>
            <NavLink to="/mcp" viewTransition className="nav-link me-4" end>MCP Servers</NavLink>
            <NavLink to="/toolbags" viewTransition className="nav-link me-4" end>ToolBags</NavLink>
            <NavLink to="/system" viewTransition className="nav-link me-4" end>System</NavLink>
            {/* <div className="d-none d-sm-flex flex-row flex-fill justify-content-end"> */}
            {/* check if production and show/hide dropdown */}
            {import.meta.env.DEV &&
                (<Dropdown isOpen={dropdownOpen} toggle={toggle} >
                    <DropdownToggle nav caret>
                        Dev
                    </DropdownToggle>
                    <DropdownMenu>
                        {/* <DropdownItem header>Header</DropdownItem> */}
                        <DropdownItem>
                            <NavLink to={OpenApiSpecUrl()} target='_blank' className="nav-link me-4" end><code>OpenAPI</code></NavLink>
                        </DropdownItem>
                        <DropdownItem divider />
                        <DropdownItem>
                            <NavLink to="/signalr" viewTransition className="nav-link me-4" end>SignalR</NavLink>
                        </DropdownItem>
                        <DropdownItem divider />
                        <DropdownItem>
                            <NavLink to="/test" viewTransition className="nav-link me-4" end>UI Tests</NavLink>
                        </DropdownItem>
                    </DropdownMenu>
                </Dropdown>)}{/* </div> */}
        </nav>
    );
};

export default Navi;
