import React, { useState, useEffect } from 'react';
import api from '../services/api';
import { Clock, CheckCircle, XCircle, MapPin, Calendar, Trash2 } from 'lucide-react';

const MyTicketsPage = () => {
  const [tickets, setTickets] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Hardcode AVG time estimates (minutes) for demo. Real app should fetch this from API.
  const AVG_TIME_MAP = {
    'DK': 5, // Đăng ký học phần
    'HS': 7, // Nộp hồ sơ
    'HP': 4, // Thanh toán học phí
    'TV': 15, // Tư vấn
    'BG': 3, // Bằng & Giấy tờ
    'DEFAULT': 5
  };

  const fetchMyTickets = async () => {
    try {
      const res = await api.get('/tickets/my');
      setTickets(res.data);
    } catch (err) {
      setError('Không thể tải danh sách vé. Vui lòng thử lại sau.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMyTickets();
    const interval = setInterval(fetchMyTickets, 10000); // Poll every 10s
    return () => clearInterval(interval);
  }, []);

  const handleCancel = async (id) => {
    if (!window.confirm('Bạn có chắc chắn muốn hủy số này?')) return;
    try {
      await api.delete(`/tickets/${id}`);
      fetchMyTickets();
    } catch (err) {
      alert('Hủy số thất bại: ' + (err.response?.data?.message || 'Lỗi hệ thống'));
    }
  };

  const getStatusColor = (status) => {
    switch(status) {
      case 'Waiting': return 'var(--warning)';
      case 'Calling': return 'var(--success)';
      case 'Completed': return 'var(--accent-primary)';
      case 'Canceled': return 'var(--danger)';
      default: return 'var(--text-secondary)';
    }
  };
  
  const getStatusIcon = (status) => {
    switch(status) {
      case 'Waiting': return <Clock size={20} />;
      case 'Calling': return <MapPin size={20} />;
      case 'Completed': return <CheckCircle size={20} />;
      case 'Canceled': return <XCircle size={20} />;
      default: return null;
    }
  };

  const getStatusText = (status) => {
    switch(status) {
      case 'Waiting': return 'Đang chờ';
      case 'Calling': return 'Đang gọi';
      case 'Completed': return 'Hoàn thành';
      case 'Canceled': return 'Đã hủy';
      default: return status;
    }
  };

  // Helper to guess ServiceCode based on ServiceName for our hardcoded estimate (Normally API should return Code or Estimate directly)
  const getEstimatedWaitTime = (serviceName, queuePosition) => {
    if (!queuePosition || queuePosition <= 0) return 0;
    
    let avg = AVG_TIME_MAP['DEFAULT'];
    if (serviceName.includes('Đăng ký')) avg = AVG_TIME_MAP['DK'];
    else if (serviceName.includes('hồ sơ')) avg = AVG_TIME_MAP['HS'];
    else if (serviceName.includes('học phí')) avg = AVG_TIME_MAP['HP'];
    else if (serviceName.includes('Tư vấn')) avg = AVG_TIME_MAP['TV'];
    else if (serviceName.includes('Bằng')) avg = AVG_TIME_MAP['BG'];

    return queuePosition * avg;
  };

  if (loading) return <div style={{ textAlign: 'center', padding: '3rem' }}>Đang tải...</div>;
  if (error) return <div style={{ color: 'var(--danger)', textAlign: 'center', padding: '2rem' }}>{error}</div>;

  return (
    <div style={{ maxWidth: '900px', margin: '0 auto' }}>
      <h2 style={{ fontSize: '1.8rem', marginBottom: '1.5rem' }}>Vé của tôi</h2>
      
      {tickets.length === 0 ? (
        <div className="glass-panel" style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
          Bạn chưa có vé hoặc lịch hẹn nào.
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {tickets.map(ticket => (
            <div key={ticket.ticketId} className="glass-panel" style={{ 
              padding: '1.5rem', 
              display: 'flex', 
              flexDirection: window.innerWidth < 768 ? 'column' : 'row',
              justifyContent: 'space-between',
              alignItems: window.innerWidth < 768 ? 'flex-start' : 'center',
              borderLeft: `4px solid ${getStatusColor(ticket.status)}`,
              gap: '1rem'
            }}>
              
              {/* Left side: Info */}
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                  <span style={{ 
                    background: 'rgba(0,0,0,0.05)', 
                    padding: '0.2rem 0.5rem', 
                    borderRadius: 'var(--radius-sm)',
                    fontSize: '0.8rem',
                    color: 'var(--text-secondary)'
                  }}>
                    {ticket.ticketType === 'WalkIn' ? 'Lấy số ngay' : 'Hẹn trước'}
                  </span>
                  <span style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
                    {new Date(ticket.createdAt).toLocaleString('vi-VN')}
                  </span>
                </div>
                
                <h3 style={{ fontSize: '1.3rem', marginBottom: '0.25rem' }}>{ticket.serviceName}</h3>
                
                {ticket.ticketType === 'WalkIn' ? (
                  <p style={{ fontSize: '1.2rem', color: 'var(--accent-primary)', fontWeight: 600 }}>
                    Số của bạn: {ticket.ticketNumber}
                  </p>
                ) : (
                  <p style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: 'var(--accent-primary)', fontWeight: 500 }}>
                    <Calendar size={18} /> Ngày hẹn: {ticket.appointmentDate ? new Date(ticket.appointmentDate).toLocaleDateString('vi-VN') : 'N/A'}
                  </p>
                )}
                
                {ticket.status === 'Calling' && ticket.counterName && (
                  <p style={{ marginTop: '0.5rem', color: 'var(--success)', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <MapPin size={18} /> Xin mời đến: {ticket.counterName}
                  </p>
                )}
              </div>
              
              {/* Right side: Status */}
              <div style={{ 
                display: 'flex', 
                flexDirection: 'column', 
                alignItems: window.innerWidth < 768 ? 'flex-start' : 'flex-end',
                minWidth: '200px'
              }}>
                <div style={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  gap: '0.5rem', 
                  color: getStatusColor(ticket.status),
                  fontWeight: 600,
                  fontSize: '1.1rem',
                  marginBottom: '0.5rem'
                }}>
                  {getStatusIcon(ticket.status)}
                  <span>{getStatusText(ticket.status)}</span>
                </div>
                
                {ticket.status === 'Waiting' && ticket.ticketType === 'WalkIn' && ticket.queuePosition > 0 && (
                  <div style={{ textAlign: window.innerWidth < 768 ? 'left' : 'right', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                    <p>Có <strong style={{ color: 'white' }}>{ticket.queuePosition}</strong> người chờ trước bạn</p>
                    <p>Ước tính: <strong style={{ color: 'var(--warning)' }}>~{getEstimatedWaitTime(ticket.serviceName, ticket.queuePosition)} phút</strong></p>
                  </div>
                )}
                
                {ticket.status === 'Waiting' && (
                  <button 
                    onClick={() => handleCancel(ticket.ticketId)}
                    style={{ 
                      marginTop: '1rem',
                      background: 'rgba(239, 68, 68, 0.1)',
                      color: 'var(--danger)',
                      border: '1px solid rgba(239, 68, 68, 0.3)',
                      padding: '0.5rem 1rem',
                      borderRadius: 'var(--radius-md)',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.25rem',
                      fontSize: '0.9rem'
                    }}
                  >
                    <Trash2 size={16} /> Hủy số
                  </button>
                )}
              </div>
              
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default MyTicketsPage;
