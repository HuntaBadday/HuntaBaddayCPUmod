using LogicAPI.Server.Components;
using System.Collections.Generic;
using System;

namespace HuntaBaddayCPUmod {
    public class TNET_Switch : LogicComponent {
        private class serial_sender {
            public serial_sender(){
                
            }
            
            // Modes:
            // idle
            // packet_start
            // byte_start
            // byte_send
            // ipg
            private string current_mode = "idle";
            private int serial_counter;
            private byte byteToSend; // Current byte to send
            
            private byte[] send_buffer = new byte[1024]; // Current buffer to be sent out the port
            private int send_position; // Buffer index
            private int send_length; // Length of the packet
            
            private List<byte[]> packet_stack = new List<byte[]>(); // All packets to be sent
            private List<int> stack_lengths = new List<int>(); // Legths of the packets
            public void reset(){
                current_mode = "idle";
                packet_stack.Clear();
                stack_lengths.Clear();
            }
            public void addPacket(byte[] buffer, int length){
                packet_stack.Add(new byte[1024]);
                stack_lengths.Add(length);
                Array.Copy(buffer, 0, packet_stack[packet_stack.Count-1], 0, 1024);
            }
            public bool doSerial(){
                bool new_state = false;
                if(current_mode == "idle"){
                    new_state = false;
                    if(packet_stack.Count > 0){
                        current_mode = "packet_start";
                    }
                }
                if(current_mode == "packet_start"){
                    Array.Copy(packet_stack[0], 0, send_buffer, 0, 1024);
                    send_position = 0;
                    send_length = stack_lengths[0];
                    packet_stack.RemoveAt(0);
                    stack_lengths.RemoveAt(0);
                    current_mode = "byte_start";
                }
                if(current_mode == "byte_start"){
                    if(send_position == send_length){
                        current_mode = "ipg";
                    } else {
                        new_state = true;
                        current_mode = "byte_send";
                        byteToSend = send_buffer[send_position++];
                    }
                    serial_counter = 0;
                } else if(current_mode == "byte_send"){
                    new_state = (byteToSend>>serial_counter & 0x1) == 1;
                    serial_counter++;
                    if(serial_counter == 8){
                        current_mode = "byte_start";
                    }
                }
                if(current_mode == "ipg"){
                    new_state = false;
                    serial_counter++;
                    if(serial_counter == 12){
                        current_mode = "idle";
                    }
                }
                return new_state;
            }
        }
        private class serial_receiver {
            public serial_receiver(){
            }
            
            // Modes:
            // idle
            // packet_start
            // start_wait
            // byte_recv
            // packet_end
            // ipg_wait
            private string current_mode = "idle";
            private int serial_counter;
            private byte byteToReceive; // Current byte to receive
            
            private byte[] receive_buffer = new byte[1024]; // Current buffer for receiving
            private int receive_position; // Buffer index
            
            private List<byte[]> packet_stack = new List<byte[]>(); // All packets to be sent
            private List<int> stack_lengths = new List<int>(); // Legths of the packets
            public void reset(){
                current_mode = "ipg_wait";
                serial_counter = 0;
                packet_stack.Clear();
                stack_lengths.Clear();
            }
            public (int length, byte[] buffer) getPacket(){
                byte[] tmp = new byte[1024];
                if(packet_stack.Count == 0){
                    return (0, tmp);
                }
                int length = stack_lengths[0];
                Array.Copy(packet_stack[0], 0, tmp, 0, 1024);
                packet_stack.RemoveAt(0);
                stack_lengths.RemoveAt(0);
                return (length, tmp);
            }
            public bool dataAvailable(){
                if(packet_stack.Count > 0){
                    return true;
                } else {
                    return false;
                }
            }
            public void doSerial(bool pin_state){
                if(current_mode == "idle"){
                    if(pin_state){
                        current_mode = "packet_start";
                    }
                }
                if(current_mode == "packet_start"){
                    receive_position = 0;
                    current_mode = "byte_recv";
                    serial_counter = 0;
                } else if(current_mode == "start_wait"){
                    if(pin_state){
                        current_mode = "byte_recv";
                        serial_counter = 0;
                    } else {
                        serial_counter++;
                        if(serial_counter == 12){
                            current_mode = "packet_end";
                        }
                    }
                } else if(current_mode == "byte_recv"){
                    byteToReceive >>= 1;
                    if(pin_state){
                        byteToReceive |= 0x80;
                    }
                    serial_counter++;
                    if(serial_counter == 8){
                        if(receive_position != 1024){
                            receive_buffer[receive_position++] = byteToReceive;
                        }
                        current_mode = "start_wait";
                        serial_counter = 0;
                    }
                }
                if(current_mode == "packet_end"){
                    uint checksum = 0;
                    uint checksumP = 0;
                    if(receive_position >= 5){
                        for(int i = 0; i < receive_position-4; i++){
                            checksum += (uint)receive_buffer[i];
                        }
                        checksumP |= (uint)(receive_buffer[receive_position-1] << 24);
                        checksumP |= (uint)(receive_buffer[receive_position-2] << 16);
                        checksumP |= (uint)(receive_buffer[receive_position-3] << 8);
                        checksumP |= (uint)(receive_buffer[receive_position-4] << 0);
                    }
                    if(receive_position >= 5 && checksum == checksumP){
                        packet_stack.Add(new byte[1024]);
                        stack_lengths.Add(receive_position);
                        Array.Copy(receive_buffer, 0, packet_stack[packet_stack.Count-1], 0, 1024);
                    }
                    current_mode = "idle";
                }
                if(current_mode == "ipg_wait"){
                    if(pin_state){
                        serial_counter = 0;
                    } else {
                        serial_counter++;
                    }
                    if(serial_counter == 12){
                        current_mode = "idle";
                    }
                }
            }
        }
        const int pin_inputs = 0;
        const int pin_outputs = 0;
        const int pin_reset = 16;
        
        const int portCount = 16;
        
        List<serial_sender> senders = new List<serial_sender>();
        List<serial_receiver> receivers = new List<serial_receiver>();
        
        List<ulong> mac_table = new List<ulong>();
        List<int> source_ports = new List<int>();
        
        protected override void Initialize(){
            for(int i = 0; i < portCount; i++){
                senders.Add(new serial_sender());
                receivers.Add(new serial_receiver());
            }
        }
        protected override void DoLogicUpdate(){
            if(getPin(pin_reset)){
                for(int i = 0; i < portCount; i++){
                    senders[i].reset();
                    receivers[i].reset();
                    setPin(pin_outputs+i, false);
                }
                mac_table.Clear();
                source_ports.Clear();
            }
            for(int i = 0; i < portCount; i++){
                bool new_state = senders[i].doSerial();
                setPin(pin_outputs+i, new_state);
                receivers[i].doSerial(getPin(pin_inputs+i));
            }
            for(int i = 0; i < portCount; i++){
                if(receivers[i].dataAvailable()){
                    var packet = receivers[i].getPacket();
                    if(packet.length < 19){
                        continue;
                    }
                    ulong source_mac = 0;
                    ulong dest_mac = 0;
                    source_mac |= (ulong)(packet.buffer[6] << 0);
                    source_mac |= (ulong)(packet.buffer[7] << 8);
                    source_mac |= (ulong)(packet.buffer[8] << 16);
                    source_mac |= (ulong)(packet.buffer[9] << 24);
                    source_mac |= (ulong)(packet.buffer[10] << 32);
                    source_mac |= (ulong)(packet.buffer[11] << 40);
                    
                    dest_mac |= (ulong)(packet.buffer[0] << 0);
                    dest_mac |= (ulong)(packet.buffer[1] << 8);
                    dest_mac |= (ulong)(packet.buffer[2] << 16);
                    dest_mac |= (ulong)(packet.buffer[3] << 24);
                    dest_mac |= (ulong)(packet.buffer[4] << 32);
                    dest_mac |= (ulong)(packet.buffer[5] << 40);
                    
                    updateTable(source_mac, i);
                    int dest_port = getPort(dest_mac);
                    if(dest_mac == 0xffffffffffff){
                        dest_port = -1;
                    }
                    if(dest_port == -1){
                        for(int j = 0; j < portCount; j++){
                            if(j != i){
                                senders[j].addPacket(packet.buffer, packet.length);
                            }
                        }
                    } else {
                        senders[dest_port].addPacket(packet.buffer, packet.length);
                    }
                }
            }
            QueueLogicUpdate();
        }
        protected int getPort(ulong mac){
            int port = -1;
            for(int i = 0; i < mac_table.Count; i++){
                if(mac_table[i] == mac){
                    port = source_ports[i];
                }
            }
            return port;
        }
        protected void updateTable(ulong mac, int new_port){
            bool found = false;
            for(int i = 0; i < mac_table.Count; i++){
                if(mac_table[i] == mac){
                    source_ports[i] = new_port;
                    found = true;
                    break;
                }
            }
            if(!found && mac_table.Count < 65536){
                mac_table.Add(mac);
                source_ports.Add(new_port);
            }
        }
        protected void setPin(int pinnum, bool state){
            base.Outputs[pinnum].On = state;
        }
        protected bool getPin(int pinnum){
            return base.Inputs[pinnum].On;
        }
    }
}