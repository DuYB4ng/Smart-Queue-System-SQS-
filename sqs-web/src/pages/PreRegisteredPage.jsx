import React, { useState, useEffect } from 'react';
import Navbar from '../components/Navbar';
import api from '../services/api';
import { Search, Calendar, CheckCircle, PlayCircle, Clock } from 'lucide-react';

const PreRegisteredPage = ({ user, onLogout }) => {
  const [appointments, setAppointments] = useState([]);
  const [selectedDate, setSelectedDate] = useState(new Date().toISOString().split('T')[0]);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const fetchAppointments = async () => {
    try {
      setLoading(true);
      const res = await api.get(`/staff/appointments?date=${selectedDate}`);
      setAppointments(res.data);
      setError('');
    } catch (err) {
      console.error(err);
      setError('Lỗi khi tải danh sách đặt trước');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAppointments();
  }, [selectedDate]);

  const handleCall = async (id) => {
    try {
      await api.post(`/staff/call-appointment/${id}`);
      fetchAppointments();
    } catch (err) {
      alert(err.response?.data?.message || 'Không thể gọi lịch hẹn này. Bạn phải hoàn thành phiên hiện tại trước.');
    }
  };

  const handleComplete = async (id) => {
    try {
      await api.post(`/staff/complete/${id}`);
      fetchAppointments();
    } catch (err) {
      alert(err.response?.data?.message || 'Lỗi khi hoàn thành.');
    }
  };

  // Lọc theo tìm kiếm
  const filteredAppointments = appointments.filter(app => 
    (app.guestName || '').toLowerCase().includes(searchQuery.toLowerCase()) ||
    (app.phoneNumber || '').includes(searchQuery) ||
    (app.studentId || '').includes(searchQuery)
  );

  return (
    <div className="app-container" style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Navbar user={user} onLogout={onLogout} />
      
      <main style={{ flex: 1, padding: '2rem', maxWidth: '1200px', margin: '0 auto', width: '100%' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
          <h2 className="text-gradient" style={{ fontSize: '1.75rem', fontWeight: 600 }}>
            Xử lý khách hàng đặt trước
          </h2>
          
          <div style={{ display: 'flex', gap: '1rem' }}>
            <div className="glass-panel" style={{ display: 'flex', alignItems: 'center', padding: '0.5rem 1rem', borderRadius: 'var(--radius-md)' }}>
              <Calendar size={18} style={{ color: 'var(--accent-primary)', marginRight: '0.5rem' }} />
              <input 
                type="date" 
                value={selectedDate}
                onChange={(e) => setSelectedDate(e.target.value)}
                style={{ background: 'transparent', border: 'none', outline: 'none', color: 'var(--text-primary)', fontFamily: 'inherit' }}
              />
            </div>

            <div className="glass-panel" style={{ display: 'flex', alignItems: 'center', padding: '0.5rem 1rem', borderRadius: 'var(--radius-md)', minWidth: '250px' }}>
              <Search size={18} style={{ color: 'var(--text-secondary)', marginRight: '0.5rem' }} />
              <input 
                type="text" 
                placeholder="Tìm tên, SĐT, MSSV..." 
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                style={{ background: 'transparent', border: 'none', outline: 'none', color: 'var(--text-primary)', width: '100%' }}
              />
            </div>
          </div>
        </div>

        {error && <div style={{ padding: '1rem', background: 'rgba(239, 68, 68, 0.1)', color: 'var(--danger)', borderRadius: 'var(--radius-md)', marginBottom: '1rem' }}>{error}</div>}

        <div className="glass-panel" style={{ overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: 'rgba(255, 255, 255, 0.05)', borderBottom: '1px solid var(--border-light)' }}>
                <th style={{ padding: '1rem', textAlign: 'left', fontWeight: 500, color: 'var(--text-secondary)' }}>Khách hàng</th>
                <th style={{ padding: '1rem', textAlign: 'left', fontWeight: 500, color: 'var(--text-secondary)' }}>Liên hệ</th>
                <th style={{ padding: '1rem', textAlign: 'left', fontWeight: 500, color: 'var(--text-secondary)' }}>Dịch vụ</th>
                <th style={{ padding: '1rem', textAlign: 'center', fontWeight: 500, color: 'var(--text-secondary)' }}>Trạng thái</th>
                <th style={{ padding: '1rem', textAlign: 'center', fontWeight: 500, color: 'var(--text-secondary)' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan="5" style={{ padding: '2rem', textAlign: 'center' }}>Đang tải...</td></tr>
              ) : filteredAppointments.length === 0 ? (
                <tr><td colSpan="5" style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-secondary)' }}>Không có lịch hẹn nào.</td></tr>
              ) : (
                filteredAppointments.map(app => (
                  <tr key={app.ticketId} style={{ borderBottom: '1px solid var(--border-light)' }}>
                    <td style={{ padding: '1rem' }}>
                      <div style={{ fontWeight: 600 }}>{app.guestName || 'Khách vãng lai'}</div>
                      {app.studentId && <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>MSSV: {app.studentId}</div>}
                    </td>
                    <td style={{ padding: '1rem' }}>{app.phoneNumber || '-'}</td>
                    <td style={{ padding: '1rem' }}>
                      <div>{app.serviceName}</div>
                      {app.note && <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Ghi chú: {app.note}</div>}
                    </td>
                    <td style={{ padding: '1rem', textAlign: 'center' }}>
                      {app.status === 'Waiting' && <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.25rem', padding: '0.25rem 0.75rem', background: 'rgba(245, 158, 11, 0.1)', color: '#f59e0b', borderRadius: '999px', fontSize: '0.875rem' }}><Clock size={14}/> Chưa xử lý</span>}
                      {app.status === 'Calling' && <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.25rem', padding: '0.25rem 0.75rem', background: 'rgba(59, 130, 246, 0.1)', color: '#3b82f6', borderRadius: '999px', fontSize: '0.875rem' }}><PlayCircle size={14}/> Đang xử lý</span>}
                      {app.status === 'Completed' && <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.25rem', padding: '0.25rem 0.75rem', background: 'rgba(34, 197, 94, 0.1)', color: '#22c55e', borderRadius: '999px', fontSize: '0.875rem' }}><CheckCircle size={14}/> Đã hoàn thành</span>}
                    </td>
                    <td style={{ padding: '1rem', textAlign: 'center' }}>
                      {app.status === 'Waiting' && (
                        <button 
                          onClick={() => handleCall(app.ticketId)}
                          style={{ padding: '0.5rem 1rem', background: 'var(--accent-primary)', color: 'white', borderRadius: 'var(--radius-md)', fontSize: '0.875rem', border: 'none', cursor: 'pointer' }}>
                          Tiếp nhận xử lý
                        </button>
                      )}
                      {app.status === 'Calling' && (
                        <button 
                          onClick={() => handleComplete(app.ticketId)}
                          style={{ padding: '0.5rem 1rem', background: 'var(--success)', color: 'white', borderRadius: 'var(--radius-md)', fontSize: '0.875rem', border: 'none', cursor: 'pointer' }}>
                          Bàn giao (Hoàn thành)
                        </button>
                      )}
                      {app.status === 'Completed' && (
                        <button disabled style={{ padding: '0.5rem 1rem', background: 'var(--border-light)', color: 'var(--text-secondary)', borderRadius: 'var(--radius-md)', fontSize: '0.875rem', border: 'none', cursor: 'not-allowed' }}>
                          Đã xong
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </main>
    </div>
  );
};

export default PreRegisteredPage;
