import React, { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import KioskPage from './pages/KioskPage';
import DisplayPage from './pages/DisplayPage';
import StaffPage from './pages/StaffPage';
import AdminPage from './pages/AdminPage';
import LoginPage from './pages/LoginPage';

function App() {
  const [user, setUser] = useState(null);

  useEffect(() => {
    // Check local storage for token on mount
    const token = localStorage.getItem('token');
    if (token) {
      try {
        // Basic JWT decode to get role (in a real app, validate token properly)
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        const decoded = JSON.parse(jsonPayload);
        
        setUser({
          id: decoded.nameid,
          name: decoded.unique_name,
          role: decoded.role
        });
      } catch (e) {
        localStorage.removeItem('token');
      }
    }
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
    setUser(null);
  };

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<KioskPage />} />
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
