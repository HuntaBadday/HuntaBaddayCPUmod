using LogicAPI.Server.Components;
using System.Collections.Generic;
using System;

namespace HuntaBaddayCPUmod {
    public class TNET_Trans_16 : LogicComponent {
        const int pin_bus = 0;
        const int pin_enable = 16;
        const int pin_read = 17;
        const int pin_write = 18;
        const int pin_rs = 19;
        const int bit_mode = 20;
        const int pin_output = 16;
        const int pin_reset = 21;
        
        const int MODE_IDLE = 0;
        const int MODE_PACKET_START = 1;
        const int MODE_BYTE_START = 2;
        const int MODE_BYTE_SEND = 3;
        const int MODE_IPG = 4;
        int current_mode = MODE_IDLE;
        int serial_counter;
        byte byteToSend; // Current byte to send
        
        byte[] send_buffer = new byte[1024]; // Current buffer to be sent out the port
        int send_position; // Buffer index
        int send_length; // Length of the packet
        
        List<byte[]> packet_stack = new List<byte[]>(); // All packets to be sent
        List<int> stack_lengths = new List<int>(); // Legths of the packets
        
        byte[] input_buffer = new byte[1024]; // Data input buffer
        int input_position = 0; // Position of the input buffer
        uint input_checksum = 0; // Checksum
        
        bool lastWritePin = false;
        protected override void Initialize(){
            
        }
        protected override void DoLogicUpdate(){
            if(getPin(pin_reset)){
                current_mode = MODE_IDLE;
                packet_stack.Clear();
                stack_lengths.Clear();
                input_position = 0;
                input_checksum = 0;
                lastWritePin = getPin(pin_write);
                writeBus(0);
                return;
            }
            if(getPin(pin_read) && getPin(pin_rs) && getPin(pin_enable)){
                ushort output = 0;
                if(current_mode == MODE_IDLE){
                    output |= 0x1;
                }
                if(input_position == 0){
                    output |= 0x2;
                }
                writeBus(output);
            } else {
                writeBus(0);
            }
            if(getPin(pin_write) && !lastWritePin && getPin(pin_rs) && getPin(pin_enable)){
                ushort value = readBus();
                if((value&0x01) != 0){
                    addPacket();
                }
                if((value&0x02) != 0){
                    input_position = 0;
                    input_checksum = 0;
                }
            } else if(getPin(pin_write) && !lastWritePin && !getPin(pin_rs) && getPin(pin_enable)){
                if(!getPin(bit_mode)){
                    byte value = (byte)readBus();
                    inputData(value);
                } else {
                    ushort value = readBus();
                    inputData((byte)value);
                    inputData((byte)(value >> 8));
                }
            }
            doSerial();
            lastWritePin = getPin(pin_write);
        }
        
        protected void addPacket(){
            if(input_position == 0){
                return;
            }
            input_buffer[input_position++] = (byte)(input_checksum >> 0 & 0xff);
            input_buffer[input_position++] = (byte)(input_checksum >> 8 & 0xff);
            input_buffer[input_position++] = (byte)(input_checksum >> 16 & 0xff);
            input_buffer[input_position++] = (byte)(input_checksum >> 24 & 0xff);
            packet_stack.Add(new byte[1024]);
            stack_lengths.Add(input_position);
            Array.Copy(input_buffer, 0, packet_stack[packet_stack.Count-1], 0, 1024);
            input_position = 0;
            input_checksum = 0;
        }
        protected void inputData(byte value){
            if(input_position == 1020){
                return;
            }
            input_buffer[input_position++] = value;
            input_checksum += value;
        }
        protected void doSerial(){
            if(current_mode == MODE_IDLE){
                setPin(pin_output, false);
                if(packet_stack.Count > 0){
                    current_mode = MODE_PACKET_START;
                }
            }
            if(current_mode == MODE_PACKET_START){
                Array.Copy(packet_stack[0], 0, send_buffer, 0, 1024);
                send_position = 0;
                send_length = stack_lengths[0];
                packet_stack.RemoveAt(0);
                stack_lengths.RemoveAt(0);
                current_mode = MODE_BYTE_START;
            }
            if(current_mode == MODE_BYTE_START){
                if(send_position == send_length){
                    current_mode = MODE_IPG;
                } else {
                    setPin(pin_output, true);
                    current_mode = MODE_BYTE_SEND;
                    byteToSend = send_buffer[send_position++];
                }
                serial_counter = 0;
                QueueLogicUpdate();
            } else if(current_mode == MODE_BYTE_SEND){
                setPin(pin_output, (byteToSend>>serial_counter & 0x1) == 1);
                serial_counter++;
                if(serial_counter == 8){
                    current_mode = MODE_BYTE_START;
                }
                QueueLogicUpdate();
            }
            if(current_mode == MODE_IPG){
                setPin(pin_output, false);
                serial_counter++;
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
        protected ushort readBus(){
            ushort output = 0;
            for(int i = pin_bus; i < pin_bus+16; i++){
                output <<= 1;
                if(base.Inputs[i].On){
                    output |= 0x1;
                }
            }
            return output;
        }
        protected void writeBus(ushort value){
            ushort output = value;
            for(int i = pin_bus; i < pin_bus+16; i++){
                base.Outputs[i].On = (output&0x1) == 1;
                output >>= 1;
            }
        }
    }
}