import { useState } from "react";
import "./styles/global.css";

import Sidebar from "./components/Sidebar";
import Dashboard from "./pages/Dashboard";
import Adesao from "./pages/Adesao";
import Carteira from "./pages/Carteira";
import GerenciarCliente from "./pages/GerenciarCliente";
import Custodia from "./pages/Custodia";
import CestaAdmin from "./pages/CestaAdmin";
import Motor from "./pages/Motor";
import Operacoes from "./pages/Operacoes";

export default function App() {
  const [page, setPage] = useState("dashboard");

  const renderPage = () => {
    switch (page) {
      case "dashboard":  return <Dashboard />;
      case "adesao":     return <Adesao />;
      case "carteira":   return <Carteira />;
      case "gerenciar":  return <GerenciarCliente />;
      case "custodia":   return <Custodia />;
      case "cesta":      return <CestaAdmin />;
      case "motor":      return <Motor />;
      case "operacoes":  return <Operacoes />;
      default:           return <Dashboard />;
    }
  };

  return (
    <div className="app">
      <Sidebar page={page} setPage={setPage} />
      <main className="main">
        {renderPage()}
      </main>
    </div>
  );
}