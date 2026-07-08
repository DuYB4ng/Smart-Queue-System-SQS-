import React, { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import KioskPage from './pages/KioskPage';
import DisplayPage from './pages/DisplayPage';
import StaffPage from './pages/StaffPage';
import AdminPage from './pages/AdminPage';
import LoginPage from './pages/LoginPage';

function App() {
  const [user, setUser] = useState(() => {
    const token = localStorage.getItem('token');
    if (token) {
      try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        const decoded = JSON.parse(jsonPayload);
        
        return {
          id: decoded.nameid || decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"],
          name: decoded.name || decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
          role: decoded.role || decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
        };
      } catch (e) {
        localStorage.removeItem('token');
      }
    }
    return null;
  });

  useEffect(() => {
    // Token validation could happen here
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
    setUser(null);
  };

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={
          user 
            ? <KioskPage user={user} onLogout={handleLogout} /> 
            : <Navigate to="/login" />
        } />
        <Route path="/display" element={<DisplayPage />} />
        <Route path="/login" element={<LoginPage onLoginSuccess={setUser} />} />
        
        <Route path="/staff" element={
          user && user.role === 'Staff' 
            ? <StaffPage user={user} onLogout={handleLogout} /> 
            : <Navigate to="/login" />
        } />
        
        <Route path="/admin" element={
          user && user.role === 'Admin' 
            ? <AdminPage user={user} onLogout={handleLogout} /> 
            : <Navigate to="/login" />
        } />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
