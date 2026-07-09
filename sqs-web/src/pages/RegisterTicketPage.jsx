import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Calendar, Clock, Ticket as TicketIcon, FileText, Phone, User, CheckCircle } from 'lucide-react';
import api from '../services/api';

const RegisterTicketPage = () => {
  const [services, setServices] = useState([]);
  const [selectedService, setSelectedService] = useState('');
  const [ticketType, setTicketType] = useState('WalkIn'); // WalkIn | Appointment
  
  // Appointment fields
  const [appointmentDate, setAppointmentDate] = useState('');
  const [studentId, setStudentId] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [note, setNote] = useState('');

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [successTicket, setSuccessTicket] = useState(null);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchServices = async () => {
      try {
        const res = await api.get('/services');
        setServices(res.data);
        if (res.data.length > 0) {
          setSelectedService(res.data[0].id.toString());
        }
      } catch (err) {
        setError('Không thể tải danh sách dịch vụ.');
      }
    };
    fetchServices();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      if (ticketType === 'WalkIn') {
        const res = await api.post('/tickets', { serviceId: parseInt(selectedService) });
        setSuccessTicket(res.data);
      } else {
        if (!appointmentDate) {
          setError('Vui lòng chọn ngày hẹn.');
          setLoading(false);
          return;
        }
        
        const res = await api.post('/tickets/appointment', { 
          serviceId: parseInt(selectedService),
          appointmentDate,
          studentId,
          phoneNumber,
          note
        });
        setSuccessTicket(res.data);
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Đăng ký thất bại. Vui lòng thử lại.');
    } finally {
      setLoading(false);
    }
  };

  const handleFinish = () => {
    navigate('/my-tickets');
  };

  if (successTicket) {
    return (
      <div className="glass-panel" style={{ maxWidth: '600px', margin: '0 auto', padding: '3rem 2rem', textAlign: 'center' }}>
        <CheckCircle size={64} style={{ color: 'var(--success)', margin: '0 auto 1.5rem' }} />
        <h2 className="text-gradient" style={{ fontSize: '2rem', marginBottom: '1rem' }}>
          {ticketType === 'WalkIn' ? 'Lấy số thành công!' : 'Đặt hẹn thành công!'}
        </h2>
        
        <div style={{ background: 'rgba(0,0,0,0.05)', padding: '2rem', borderRadius: 'var(--radius-md)', margin: '2rem 0' }}>
          {ticketType === 'WalkIn' && (
            <>
              <p style={{ color: 'var(--text-secondary)', marginBottom: '0.5rem' }}>Số thứ tự của bạn</p>
              <h1 style={{ fontSize: '4rem', color: 'var(--accent-primary)', lineHeight: 1 }}>{successTicket.ticketNumber}</h1>
            </>
          )}
          
          <h3 style={{ fontSize: '1.5rem', marginTop: '1.5rem' }}>Dịch vụ: {successTicket.serviceName}</h3>
          
          {ticketType === 'WalkIn' && (
            <p style={{ marginTop: '1rem', color: 'var(--warning)' }}>
              Đang có {successTicket.estimatedWait} người chờ trước bạn
            </p>
          )}
          
          {ticketType === 'Appointment' && (
            <p style={{ marginTop: '1rem', color: 'var(--accent-primary)' }}>
              Ngày hẹn: {new Date(successTicket.appointmentDate).toLocaleDateString('vi-VN')}
            </p>
          )}
        </div>

        <button 
          onClick={handleFinish}
          style={{ padding: '1rem 2rem', background: 'var(--accent-primary)', color: 'white', borderRadius: 'var(--radius-full)', fontWeight: 600 }}
        >
          Xem vé của tôi
        </button>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '600px', margin: '0 auto' }}>
      <div className="glass-panel" style={{ padding: '2rem' }}>
        <h2 style={{ fontSize: '1.5rem', marginBottom: '1.5rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <TicketIcon className="text-gradient" /> Đăng ký Dịch vụ
        </h2>

        {error && (
          <div style={{ background: 'rgba(239, 68, 68, 0.1)', color: 'var(--danger)', padding: '1rem', borderRadius: 'var(--radius-md)', marginBottom: '1.5rem' }}>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: '1.5rem' }}>
            <label style={{ display: 'block', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>Chọn Dịch vụ</label>
            <select 
              value={selectedService} 
              onChange={e => setSelectedService(e.target.value)}
              required
            >
              {services.map(s => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </div>

          <div style={{ marginBottom: '2rem' }}>
            <label style={{ display: 'block', marginBottom: '1rem', color: 'var(--text-secondary)' }}>Hình thức</label>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div 
                onClick={() => setTicketType('WalkIn')}
                style={{ 
                  padding: '1.5rem 1rem', 
                  border: `2px solid ${ticketType === 'WalkIn' ? 'var(--accent-primary)' : 'var(--border-light)'}`,
                  borderRadius: 'var(--radius-md)',
                  textAlign: 'center',
                  cursor: 'pointer',
                  background: ticketType === 'WalkIn' ? 'rgba(15, 118, 110, 0.1)' : 'transparent'
                }}
              >
                <Clock size={24} style={{ margin: '0 auto 0.5rem', color: ticketType === 'WalkIn' ? 'var(--accent-primary)' : 'var(--text-secondary)' }} />
                <h4 style={{ color: ticketType === 'WalkIn' ? 'white' : 'var(--text-secondary)' }}>Lấy số ngay</h4>
                <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>Xếp hàng và chờ gọi số ngay hôm nay</p>
              </div>

              <div 
                onClick={() => setTicketType('Appointment')}
                style={{ 
                  padding: '1.5rem 1rem', 
                  border: `2px solid ${ticketType === 'Appointment' ? 'var(--accent-primary)' : 'var(--border-light)'}`,
                  borderRadius: 'var(--radius-md)',
                  textAlign: 'center',
                  cursor: 'pointer',
                  background: ticketType === 'Appointment' ? 'rgba(15, 118, 110, 0.1)' : 'transparent'
                }}
              >
                <Calendar size={24} style={{ margin: '0 auto 0.5rem', color: ticketType === 'Appointment' ? 'var(--accent-primary)' : 'var(--text-secondary)' }} />
                <h4 style={{ color: ticketType === 'Appointment' ? 'white' : 'var(--text-secondary)' }}>Đặt hẹn trước</h4>
                <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>Chọn ngày hẹn cho tương lai</p>
              </div>
            </div>
          </div>

          {ticketType === 'Appointment' && (
            <div style={{ background: 'rgba(0,0,0,0.05)', padding: '1.5rem', borderRadius: 'var(--radius-md)', marginBottom: '2rem' }}>
              <h4 style={{ marginBottom: '1rem', color: 'var(--accent-primary)' }}>Thông tin bổ sung</h4>
              
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>Ngày hẹn <span style={{color:'var(--danger)'}}>*</span></label>
                <input 
                  type="date" 
                  value={appointmentDate}
                  onChange={e => setAppointmentDate(e.target.value)}
                  min={new Date().toISOString().split('T')[0]}
                  required
                />
              </div>

              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>Mã số sinh viên (Tùy chọn)</label>
                <div style={{ position: 'relative' }}>
                  <User size={16} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
                  <input type="text" value={studentId} onChange={e => setStudentId(e.target.value)} style={{ paddingLeft: '2.5rem' }} placeholder="Nhập MSSV nếu có" />
                </div>
              </div>

              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>Số điện thoại (Tùy chọn)</label>
                <div style={{ position: 'relative' }}>
                  <Phone size={16} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
                  <input type="tel" value={phoneNumber} onChange={e => setPhoneNumber(e.target.value)} style={{ paddingLeft: '2.5rem' }} placeholder="Nhập số điện thoại" />
                </div>
              </div>

              <div>
                <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>Ghi chú (Tùy chọn)</label>
                <div style={{ position: 'relative' }}>
                  <FileText size={16} style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
                  <input type="text" value={note} onChange={e => setNote(e.target.value)} style={{ paddingLeft: '2.5rem' }} placeholder="Lý do, yêu cầu..." />
                </div>
              </div>
            </div>
          )}

          <button 
            type="submit" 
            disabled={loading}
            style={{ 
              width: '100%', 
              padding: '1rem', 
              background: 'var(--accent-gradient)', 
              color: 'white', 
              borderRadius: 'var(--radius-md)', 
              fontWeight: 600, 
              fontSize: '1.1rem',
              opacity: loading ? 0.7 : 1,
              boxShadow: 'var(--shadow-glow)'
            }}
          >
            {loading ? 'Đang xử lý...' : (ticketType === 'WalkIn' ? 'LẤY SỐ NGAY' : 'ĐẶT LỊCH HẸN')}
          </button>
        </form>
      </div>
    </div>
  );
};

export default RegisterTicketPage;
