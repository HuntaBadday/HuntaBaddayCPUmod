using LogicAPI.Server.Components;
using System.Collections.Generic;
using System;

namespace HuntaBaddayCPUmod {
    public class TNET_Recv2 : LogicComponent {
        public override bool HasPersistentValues => true;
        
        const int pin_bus = 0;
        const int pin_enable = 8;
        const int pin_read = 9;
        const int pin_write = 10;
        const int pin_rs = 11;
        const int pin_input = 12;
        const int pin_reset = 13;
        const int pin_empty = 8;
        const int pin_interrupt = 9;
        
        const int MODE_IDLE = 0;
        const int MODE_PACKET_START = 1;
        const int MODE_START_WAIT = 2;
        const int MODE_BYTE_RECV = 3;
        const int MODE_PACKET_END = 4;
        const int MODE_IPG_WAIT = 5;
        int current_mode = MODE_IPG_WAIT;
        
        int serial_counter;
        byte byteToReceive; // Current byte to receive
        
        byte[] receive_buffer = new byte[1024]; // Current buffer for receiving
        int receive_position; // Buffer index
        
        List<byte[]> packet_stack = new List<byte[]>(); // All packets to be sent
        List<int> stack_lengths = new List<int>(); // Legths of the packets
        
        byte[] output_buffer = new byte[1024]; // Data input buffer
        int output_position = 0; // Position of the output buffer
        int output_length = 0; // Length of the output buffer
        
        bool lastWritePin = false;
        bool hasRead = false;
        bool interrupt_enable = false;
        protected override void Initialize(){
            
        }
        protected override void DoLogicUpdate(){
            if(getPin(pin_reset)){
                current_mode = MODE_IPG_WAIT;
                serial_counter = 0;
                packet_stack.Clear();
                stack_lengths.Clear();
                lastWritePin = getPin(pin_write);
                output_length = 0;
                output_position = 0;
                writeBus(0);
                setPin(pin_empty, false);
                setPin(pin_interrupt, false);
                interrupt_enable = false;
                return;
            }
            if(getPin(pin_read) && getPin(pin_rs) && getPin(pin_enable)){
                byte output = 0;
                if(packet_stack.Count > 0){
                    output |= 0x1;
                }
                if(output_position == output_length){
                    output |= 0x2;
                }
                if(packet_stack.Count > 0 && interrupt_enable){
                    output |= 0x80;
                }
                writeBus(output);
                hasRead = false;
            } else if(getPin(pin_read) && !getPin(pin_rs) && getPin(pin_enable)){
                if(!hasRead){
                    if(output_position != output_length){
                        writeBus(output_buffer[output_position++]);
                        setPin(pin_empty, false);
                    } else {
                        writeBus(0);
                        setPin(pin_empty, true);
                    }
                }
                hasRead = true;
            } else {
                hasRead = false;
                writeBus(0);
                setPin(pin_empty, false);
            }
            if(getPin(pin_write) && !lastWritePin && getPin(pin_rs) && getPin(pin_enable)){
                byte value = readBus();
                if((value&0x1) != 0){
                    if(packet_stack.Count != 0 && (value&0x4) != 0){
                        output_position = 0;
                        output_length = stack_lengths[0];
                        Array.Copy(packet_stack[0], 0, output_buffer, 0, 1024);
                        packet_stack.RemoveAt(0);
                        stack_lengths.RemoveAt(0);
                    } else if(packet_stack.Count != 0 && (value&0x4) == 0){
                        output_position = 0;
                        output_length = stack_lengths[0]-4;
                        Array.Copy(packet_stack[0], 0, output_buffer, 0, 1020);
                        packet_stack.RemoveAt(0);
                        stack_lengths.RemoveAt(0);
                    } else {
                        output_length = 0;
                        output_position = 0;
                    }
                }
                if((value&0x2) != 0){
                    packet_stack.Clear();
                    stack_lengths.Clear();
                    output_position = 0;
                    output_length = 0;
                }
                if((value&0x8) != 0){
                    interrupt_enable = true;
                }
                if((value&0x10) != 0){
                    interrupt_enable = false;
                }
            } else if(getPin(pin_write) && !lastWritePin && !getPin(pin_rs) && getPin(pin_enable)){
                // Do nothing with this
            }
            doSerial();
            lastWritePin = getPin(pin_write);
            
            if(interrupt_enable){
                setPin(pin_interrupt, packet_stack.Count>0);
            } else {
                setPin(pin_interrupt, false);
            }
            
        }
        
        protected void doSerial(){
            if(current_mode == MODE_IDLE){
                if(getPin(pin_input)){
                    current_mode = MODE_PACKET_START;
                }
            }
            if(current_mode == MODE_PACKET_START){
                receive_position = 0;
                QueueLogicUpdate();
                current_mode = MODE_BYTE_RECV;
                serial_counter = 0;
            } else if(current_mode == MODE_START_WAIT){
                if(getPin(pin_input)){
                    current_mode = MODE_BYTE_RECV;
                    serial_counter = 0;
                } else {
                    serial_counter++;
                    if(serial_counter == 12){
                        current_mode = MODE_PACKET_END;
                    }
                }
                QueueLogicUpdate();
            } else if(current_mode == MODE_BYTE_RECV){
                byteToReceive >>= 1;
                if(getPin(pin_input)){
                    byteToReceive |= 0x80;
                }
                serial_counter++;
                if(serial_counter == 8){
                    if(receive_position != 1024){
                        receive_buffer[receive_position++] = byteToReceive;
                    }
                    current_mode = MODE_START_WAIT;
                    serial_counter = 0;
                }
                QueueLogicUpdate();
            }
            if(current_mode == MODE_PACKET_END){
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
                current_mode = MODE_IDLE;
            }
            if(current_mode == MODE_IPG_WAIT){
                if(getPin(pin_input)){
                    serial_counter = 0;
                } else {
                    serial_counter++;
                }
                if(serial_counter == 12){
                    current_mode = MODE_IDLE;
                }
                QueueLogicUpdate();
            }
        }
        
        protected void setPin(int pinnum, bool state){
            base.Outputs[pinnum].On = state;
        }
        protected bool getPin(int pinnum){
            return base.Inputs[pinnum].On;
        }
        protected byte readBus(){
            byte output = 0;
            for(int i = pin_bus; i < pin_bus+8; i++){
                output <<= 1;
                if(base.Inputs[i].On){
                    output |= 0x1;
                }
            }
            return output;
        }
        protected void writeBus(byte value){
            byte output = value;
            for(int i = pin_bus; i < pin_bus+8; i++){
                base.Outputs[i].On = (output&0x1) == 1;
                output >>= 1;
            }
        }
        
        protected override byte[] SerializeCustomData() {
            byte[] data = new byte[1];
            data[0] = interrupt_enable ? (byte)0xff : (byte)0x00;
            return data;
        }
        
        protected override void DeserializeData(byte[] data) {
            if (data == null)
                return;
            interrupt_enable = data[0] == 0xff;
        }
    }
}