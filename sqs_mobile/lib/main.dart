import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:signalr_netcore/signalr_client.dart';

void main() {
  runApp(const SQSMobileApp());
}

class SQSMobileApp extends StatelessWidget {
  const SQSMobileApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SQS Mobile',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.indigo),
        useMaterial3: true,
      ),
      home: const TicketTrackerPage(),
      debugShowCheckedModeBanner: false,
    );
  }
}

class TicketTrackerPage extends StatefulWidget {
  const TicketTrackerPage({super.key});

  @override
  State<TicketTrackerPage> createState() => _TicketTrackerPageState();
}

class _TicketTrackerPageState extends State<TicketTrackerPage> {
  final TextEditingController _ticketIdController = TextEditingController();
  
  HubConnection? _hubConnection;
  bool _isConnected = false;
  
  Map<String, dynamic>? _ticketData;
  String _statusMessage = 'Nhập Ticket ID của bạn (ví dụ: 1, 2, ...)';
  bool _isLoading = false;

  final String _baseUrl = 'http://10.0.2.2:5000'; // 10.0.2.2 is localhost for Android Emulator

  @override
  void initState() {
    super.initState();
    _initSignalR();
  }

  Future<void> _initSignalR() async {
    _hubConnection = HubConnectionBuilder()
        .withUrl('$_baseUrl/hubs/queue')
        .withAutomaticReconnect()
        .build();

    _hubConnection?.onclose(({error}) {
      setState(() => _isConnected = false);
      print("SignalR Connection Closed");
    });

    _hubConnection?.on('TicketStatusChanged', _handleTicketStatusChanged);

    try {
      await _hubConnection?.start();
      setState(() => _isConnected = true);
      print("SignalR Connected!");
    } catch (e) {
      print("SignalR Connection Error: $e");
    }
  }

  void _handleTicketStatusChanged(List<Object?>? arguments) {
    if (arguments == null || arguments.isEmpty) return;
    
    final data = arguments.first as Map<String, dynamic>;
    
    // Only update if it's the ticket we're currently tracking
    if (_ticketData != null && data['ticketId'] == _ticketData!['ticketId']) {
      setState(() {
        _ticketData!['status'] = data['newStatus'];
        
        if (data['newStatus'] == 'Calling') {
          _statusMessage = data['message'] ?? 'Đến quầy ${data['counterName']} ngay!';
        } else if (data['newStatus'] == 'Completed') {
          _statusMessage = 'Đã phục vụ xong. Cảm ơn bạn!';
        } else if (data['newStatus'] == 'Canceled') {
          _statusMessage = data['message'] ?? 'Phiên đã bị hủy.';
        }
      });
    }
  }

  Future<void> _trackTicket() async {
    final ticketIdStr = _ticketIdController.text.trim();
    if (ticketIdStr.isEmpty) return;

    final ticketId = int.tryParse(ticketIdStr);
    if (ticketId == null) {
      setState(() => _statusMessage = 'Ticket ID phải là một số hợp lệ.');
      return;
    }

    setState(() {
      _isLoading = true;
      _statusMessage = 'Đang tìm kiếm...';
    });

    try {
      // Unsubscribe from previous group if any
      if (_ticketData != null && _isConnected) {
        await _hubConnection?.invoke('LeaveGroup', args: ['ticket-${_ticketData!['ticketId']}']);
      }

      // We should ideally have an API endpoint to GET ticket details by ID.
      // Since Phase 4 only has POST /api/tickets/guest, we might need a GET endpoint.
      // Assuming GET /api/tickets/{id} exists or we simulate it.
      // For this implementation plan, we just assume the API exists or we rely completely on SignalR.
      
      // Since we don't have a GET /api/tickets/{id} explicitly defined in earlier phases,
      // we'll just join the SignalR group and show a waiting state.
      
      if (_isConnected) {
        await _hubConnection?.invoke('JoinGroup', args: ['ticket-$ticketId']);
        
        setState(() {
          _ticketData = {
            'ticketId': ticketId,
            'status': 'Waiting (Tracking)',
          };
          _statusMessage = 'Đang theo dõi Ticket #$ticketId.\nHệ thống sẽ thông báo khi đến lượt bạn.';
          _isLoading = false;
        });
      } else {
        setState(() {
          _statusMessage = 'Không thể kết nối đến máy chủ thời gian thực.';
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        _statusMessage = 'Có lỗi xảy ra: $e';
        _isLoading = false;
      });
    }
  }

  @override
  void dispose() {
    _hubConnection?.stop();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Theo dõi Xếp hàng', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        actions: [
          Icon(
            _isConnected ? Icons.wifi : Icons.wifi_off,
            color: _isConnected ? Colors.green : Colors.red,
          ),
          const SizedBox(width: 16),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Icon(Icons.confirmation_number_outlined, size: 80, color: Colors.indigo),
            const SizedBox(height: 24),
            TextField(
              controller: _ticketIdController,
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                labelText: 'Nhập Ticket ID của bạn',
                border: const OutlineInputBorder(),
                prefixIcon: const Icon(Icons.search),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.arrow_forward),
                  onPressed: _isLoading ? null : _trackTicket,
                ),
              ),
              onSubmitted: (_) => _isLoading ? null : _trackTicket(),
            ),
            const SizedBox(height: 32),
            
            if (_ticketData != null) ...[
              Card(
                elevation: 4,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                child: Padding(
                  padding: const EdgeInsets.all(24.0),
                  child: Column(
                    children: [
                      Text(
                        'TICKET ID: ${_ticketData!['ticketId']}',
                        style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.indigo),
                      ),
                      const Divider(height: 32),
                      _buildStatusIcon(),
                      const SizedBox(height: 16),
                      Text(
                        _statusMessage,
                        textAlign: TextAlign.center,
                        style: const TextStyle(fontSize: 18),
                      ),
                    ],
                  ),
                ),
              )
            ] else ...[
              Text(
                _statusMessage,
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.grey, fontSize: 16),
              ),
            ]
          ],
        ),
      ),
    );
  }

  Widget _buildStatusIcon() {
    final status = _ticketData?['status'] ?? '';
    
    if (status == 'Calling') {
      return const Column(
        children: [
          Icon(Icons.campaign, size: 64, color: Colors.orange),
          SizedBox(height: 8),
          Text('ĐẾN LƯỢT CỦA BẠN!', style: TextStyle(color: Colors.orange, fontWeight: FontWeight.bold, fontSize: 20)),
        ],
      );
    } else if (status == 'Completed') {
      return const Icon(Icons.check_circle, size: 64, color: Colors.green);
    } else if (status == 'Canceled') {
      return const Icon(Icons.cancel, size: 64, color: Colors.red);
    } else {
      return const CircularProgressIndicator();
    }
  }
}
