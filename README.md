# HuntaBaddayCPUmod

A mod for Logic World that adds processors and other useful microchips!\
Please create an issue or ping/message me (huntabadday6502 on discord) for ANY questions or problems you have. (Related to this mod). You may also request new components.

### Included components
- LWC3.1 16 bit microprocessor
- MOS 6502 microprocessor
- 8 and 16 bit FIFO (First in / First out) buffers (64k words each)
- TSC-6530 Dual timer chip
- TurnerNet network transmitter/receiver
- TurnerNet network switch

## LWC 3.1

### Helpful Tools:
Instruction Documentation: https://docs.google.com/spreadsheets/d/15QtVbXtz8gos2C4d7pLwn_2SpXMISwSytKTilIrm0DM/edit#gid=0

CustomASM assembler: https://github.com/hlorenzi/customasm \
CustomASM instruction definitions: lwc31.asm\
lwc31.asm also replaces instructions like "ALU" and "JIF" with thier expansions.

### Usage:

#### CPU I/O:
AAAAAAAAAAAAAAAA WR DDDDDDDDDDDDDDDD TSC BU I\
^\
LSB

A: Address Bus\
W: Write\
R: Read\
D: Data bus (Make sure to connect the upper pins to the lower pins, this is so the cpu can have bi-directional I/O).\
T: Turbo\
S: Reset\
C: Clock In\
B: High when CPU is using the BUS\
U: Unused\
I: Trigger interrupt

Least significant bit of address and data is in the left when looking at it.

##### What is turbo??

The way the actual CPU (The one I made in logic world which this mod simulates) works is that each instruction always takes 2 clock cycles, that is because the second clock cycle is to execute the instruction (Amazing I even got stack operations in one cycle), for example MOV, the second cycle is to execute it. But because this is a mod, I added the turbo pin which removes the second cycle for instructions that don't need it (Instructions that don't need the data bus). This will make the CPU run up to 2x faster depending on program.

#### Programming:
##### General:
###### Reset cycle:
This cycle is triggered as long as the reset pin is high.\
The cycle loads the start address from address 0XFFFF and disables interrups.\
Note: Registers are not cleared.

###### Fetch-Execute Cycle:
1. Fetch instruction
2. Fetch instruction data (If needed)
3. Execute

The data is only fetched when the instruction needs it.

###### Registers:
In the LWC 3.1 there are 8 registers:

0. Const (Automatically loaded if needed by instruction)
1. r0 (General perpose)
2. r1 (GP)
3. r2 (GP)
4. r3 (GP)
5. r4 (GP)
6. SP (Stack pointer)
7. ST (Status)(Read only)

More info can be found on the google sheets document.

##### Machine Code:
Instructions are in the format:\
IIIIXXXYYYZZZZNN DDDDDDDDDDDDDDDD\
^\
MSB

Where:\
I: Instruction\
X: Operand 1 register selector\
Y: Operand 2 register selector\
Z: Operand 3 index register selector / Instruction options\
N: Unused

D: Data (The next 16 bits following the instruction word)

Do not add the data if the instruction doesn't use it.

##### Assembler (CustomASM):
You will need to learn how to use CustomASM by reading it's wiki.

Include the lwc31.asm in your code to add the instruction definitions.
"lwc31.asm" Replaces the following:
###### ALU:
Replaced by:\
ADD\
ADC\
SUB\
SBC\
SHL\
ROL\
SHR\
ROR\
AND\
OR\
XOR

###### JIF:
Replaced by:\
BEQ (Branch if equal / Brach if zero)\
BNE (Branch if not equal / Brach if not zero)\
BCS (Branch if carry set)\
BCC (Branch if carry clear)\
BMI (Branch if minus)\
BPL (Branch if plus / positive)

The branching works like the 6502 (really just the naming scheme), see http://www.6502.org/tutorials/compare_instructions.html to better understand using it with the CMP instruction (compare is really just a non-storing subtraction).

================================================================

"lwc31.asm" Adds the following to commands:
###### LOD
Adds abbreviations:\
mov REG, [ADDR]\
mov REG, [ADDR+REG]\
mov REG, [REG+REG]

###### STO:
Adds abbreviations:\
mov [ADDR], REG\
mov [ADDR+REG], REG\
mov [REG+REG], REG

###### INT:
Adds abbreviations:\
CLI (Clear interrupt diable)\
SEI (Set interrupt disable)

### Examples:
###### Multiplication (Shift and add method)
```x86
#include "lwc31.asm"

#addr 0x0000
init:
    ; Load multiplier and multiplicant
    mov r1, [data]
    mov r2, [data+1]
    mov r3, 0
main:
    mov r0, r1  ; Store r1 to r0 to do bit checking
    and r0, 0x1
    beq .skipAdd    ; If bit 0 is not set skip the add
    add r3, r2
.skipAdd:
    shl r2  ; Shift the registers
    shr r1
    bne main    ; If r1 is 0 then we are done, otherwise keep looping
    
    mov [0x8000], r3    ; Store the result into 0x8000
    jmp $
    
data:
#d16 23, 8

#addr 0xffff
#d16 init   ; Start vector pointing to init
```

###### Interrupts
```x86
#addr 0x0000

#include "../lwc31.asm"

init:
    ; Initialize interrupts and stack
    mov sp, 0xffff
    int irq ; Set interrupt location
    cli     ; Enable interupts
    
    ; Some code that loops to show the interrupts working.
    ; (Hook up a write-only register to 0x8000 to see the data)
    mov r1, 1
    mov [0x8000], r1
loop:
    mov r1, [0x8000]
    rol r1
    mov [0x8000], r1
    jmp loop
    
irq:
    pushall ; Push all registers (including the status), except stack pointer.
            ; So when the interrupt returns, the code that was running doesn't see any changes.
    
    ; Shift the value in memory right
    mov r1, [0x8000]
    ror r1
    cmp r1, 0   ; Because the carry flag isn't saved in the irq the shift won't loop around.
                ; This means that if it's 0 we set the MSB.
    bne .skip
    mov r1, 0x8000  
.skip:
    mov [0x8000], r1
        
    popall  ; Restore the registers
    rti

#addr 0xffff
#d16 init   ; Start vector pointing to init
```

## 8, 16 bit FIFO buffers
### Usage
#### Front
- First 8/16 inputs is the data input.
- Next output is a status for if the buffer is full.
- Next input is the write pin.

#### Back
- First 8/16 output is the data output.
- Next output is the status for if data is available.
- Next input is a read pin.

#### Read / Write
To write, put the data on the inputs, the data will be written on the rising edge of the write pin. Data will be read to the outputs on the rising edge of the read pin.

## TSC6530 interval timer chip (Basically the timer part of the MOS6526 Complex Interface Adapter)
### Features
- Two 16 bit internal timers
- Multiple operation modes
- Multiple interrupt options

### Pinout (Starts from the left)
#### Front
0 - 7 = Data bus (MSB Right, pin 7) (Upper and Lower Pins)\
8 = Chip select\
9 = Read\
10 = Write\
11 - 13 = Address

#### Back
0 - Ext 1\
1 - Ext 2\
2 - Ext 3\
4 - CNT\
5 - TA (Timer A output)\
6 - TB (Timer B output)\
7 - IR (Interrupt)\
8 - RES (Reset)\
9 - Clk (System clock)


### Internal register map
```
0 - Timer A: Low Byte
1 - Timer A: High Byte
2 - Timer B: Low Byte
3 - Timer B: High Byte
4 - Interrupt Control Register (Read IRs / Write mask)
    7 - IR flag (1 = IR Occured / Set-_Clear flag)
    6 - Unused
    5 - Unused
    4 - Ext 3
    3 - Ext 2
    2 - Ext 1
    1 - Timer B Interrupt
    0 - Timer A Interrupt
5 - Control Register A
    5 - Timer A Counts: 1 = CNT Signals, 0 = System Clock
    4 - Force Load Timer A: 1 = Yes
    3 - Timer A Run Mode: 1 = One-Shot, 0 = Continuous
    2 - Timer A Output mode: 1 = Toggle, 0 = Pulse
    1 - Timer A Output: 1 = Yes
    0 - Start / Stop Timer A: 1 = Start, 0 = Stop
6 - Control Register B:
    6-5 - Timer B Mode Select
        00 = Count System Clock Pulses
        01 = Count Positive CNT Transitions
        10 = Count Timer A Underflow Pulses
        11 = Count Timer A Underflows While CNT Positive
    4-0 - Same as Control Reg. A-for Timer B
```

### Interval Timers
Each interval consists of a 16-bit read-only Timer Counter and a 16-bit write only Timer Latch. Data written to the timer are latched in the Timer Latch, while data read from the timer are the present contents of the Time Counter. The timers can be used independently or linked for extended operations. The various timer modes allow generation of time delays and variable frequency output. Utilizing the CNT input, the timers can count external pulses or measure frequency, pulse width and delay times of external signals. Each time has an associated control register, providing independant control of the following functions:

#### Start/Stop
A control bit allows the timer to be started and stopped.

#### Output On/Off
A control bit allows the timer output to appear on the output.

#### Toggle/Pulse
A control bit selects the type of output. On every timer underflow the output can either toggle or generate a single positive pulse of one cycle duration. The toggle output is set high whenever the timer is started and is set low by RES.

#### One-Shot/Continous
A control bit selects either timer mode. In one-shot mode, the timer will count down from the latched value to zero, generate an interrupt, reload the latched value, then stop. In continuous mode, the timer will count from the latched value to zero, generate the interrupt, reload the latched value and repeat the procedure continuously.

#### Force Load
A strobe bit allows the timer latch to be loaded into the timer counter at any time, whether the timer is running or not.

#### Input Mode
Control bits allow the selection of the clock used to decrement the timer. TIMER A can count system clock pulses or external pulses applied to the CNT pin. TIMER B can count system clock pulses, external CNT pulses, TIMER A underflow pulses or TIMER A underflow pulses while the CNT pin is held high. The timer latch is loaded into the timer on any timer underflow, on a force load or following a write to the latches while the timer is stopped. If the timer is running, a write to the timer byte will load the timer latch, but not reload the counter.

### Interrupt Control Register
There are five sources of an interrupt: underflow from TIMER A, underflow from TIMER B or any transition from low to high on the EXT pins. A single register provides masking and interrupt information. The Interrupt Control Register consists of a write-only MASK register and a read-only data register. Any interrupt will set the corresponding bit in the DATA register. Any interrupt which is enabled by the MASK register will set the IR bit (MSB) of the data register and set the IR pin high. The interrupt DATA register is cleared and the IR line returns low following a read of the DATA register.\
\
The MASK register provides convinient control of individual mask bits. When writing to the MASK register, if bit 7 (SET/_CLEAR) of the data is a ZERO, any mask bit written with a one will be cleared, while those mask bits written with a zero will be unaffected. if bit 7 of the data written is a ONE, any mask bits written with a one will be set, while those mask bits written with a zero will be unaffected. In order for an interrupt flag to set IR and generate an interrupt, the corresponding  MASK bit must be set.

#### Other notes
- Make sure to connect the data bus outputs to the data bus inputs! (If you need data from the thing)

## TurnerNet
### Overview
TurnerNet is a L2 networking protocol for connecting devices and computers to eachother. The packets are variable length up to 1024 bytes total.

### Serial protocol
A tnet packet is started with a high signal for one tick, followed by the data, each octet of the packet is separated by a "1". Each packet must be separated by a gap of 10 ticks. Least significant bit is sent first.

### Network protocol
The tnet packet is composed of 5 sections, target address, source address, type, payload and checksum.\
Packet form:
```
+----------+----------+---------+---------------+----------+
| Target   | Source   | Type    | Payload       | Checksum |
+----------+----------+---------+---------------+----------+
| 2 octets | 2 octets | 2 octet | 1-1014 octets | 4 octets |
+----------+----------+---------+---------------+----------+
```

### TNET Transmitter
The transmitter converts a packet of data into a serial data stream to be received by another device. It has an internal buffer of 1024 bytes. A checksum will be calculated automatically.

#### I/O
The left of the device is the serial data output.\
On the front, in order from left to right is the data I/O (LSB Right), chip select / enable, read, write, RS.\
On the right is a reset pin.

### Data Input
Writing to the chip while the RS pin is low will append data to the internal buffer.

### Control Register
The control register can be accessed while the RS pin is high.

#### Control Register (Read / Write)
- 0: Has finished sending / Send packet
- 1: Is buffer empty / Clear buffer
- 2: Not used
- 3: Not used
- 4: Not used
- 5: Not used
- 6: Not used
- 7: Not used

### TNET Receiver
The receiver receives a serial stream and convert it into readable data. It has an internal buffer of 1024 bytes. A checksum is calculated automatically and will ignore the packet if the packet's checksum do not match.

#### I/O
The left of the device is the serial data input and a status output when reading the buffer.\
On the front, in order from left to right is the data I/O (LSB Right), chip select / enable, read, write, RS.\
On the right is a reset pin.

#### Data Output
Reading from the chip while the RS pin is low will read an octet from the internal buffer.\
The status pin will turn on when the buffer is empty while reading, this pin isn't strictly needed for operation since there is the "buffer empty" bit in the control register.

### Control Register
The control register can be accessed while the RS pin is high.

#### Control Register (Read / Write)
- 0: Packet available / Start reading next packet
- 1: Is buffer empty / Clear packet stack & clear buffer
- 2: _ / Include checksum at the end of the data
- 3: Not used
- 4: Not used
- 5: Not used
- 6: Not used
- 7: Not used

## MOS 6502
A microprocessor designed by MOS Technologies in 1975. This component is meant to simulate as closely as possible to the real 6502's timings. Please report any problems such as bugs or timing issues.

### Differences
- N and V flags may not be set correctly when using ADC or SBC in decimal mode.

### Pinout
<img src="https://ist.uwaterloo.ca/~schepers/MJK/pics/6502.gif" width=300>

## Multiplexers and demultiplexers
Multiplexers and demultiplexers to route signals.

### Pinout
On the front are 6 pins. From right to left is:
- Input ot output
- Output disable
- Selector bit 3
- Selector bit 2
- Selector bit 1
- Selector bit 0

On the back are 16 pins for the multiplexed signal.