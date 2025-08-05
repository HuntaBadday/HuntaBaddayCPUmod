# HuntaBaddayCPUmod

A mod for Logic World that adds processors and other useful microchips!\
Please create an issue or ping/message me (huntabadday6502 on discord) for ANY questions or problems you have. (Related to this mod). You may also request new components.

### Installation
Copy the ``HuntaBaddayCPUmod`` folder to ``"Logic World/GameData/"``

### Required mods
- HuntasIntegratedCircuitLib (HuntasICLib)
- EccsLogicWorldAPI
- EccsGuiBuilder

### Included components
- LWC31 16 bit microprocessor
- LWC33 16 bit microprocessor
- MOS 6502 microprocessor
- 16 bit x 16 bit address RAM
- 8 and 16 bit FIFO (First in / First out) buffers (64k words each)
- TSC-6530 Dual timer chip
- TurnerNet network transmitter/receiver
- TurnerNet network switch
- 4 bit multiplexers and demultiplexers
- TSC-6540 Video chip
- Terminal Controller for CheeseUtil text display
- TSC3301 (LWC33 based microcontroller)

### Credits
- CheeseUtilMod's code for helping me figure out the custom data and file loading system.

## LWC 31
### Helpful Tools:
CustomASM assembler: https://github.com/hlorenzi/customasm \
CustomASM instruction definitions: lwc31.asm\
lwc31.asm also replaces instructions like "ALU", "JIF" and "INT" with thier expansions, also adds abbreviations for LOD and STO, as well as macros for clearing and setting the carry.

LWC31 Processor documentation: TSC-LWC31.pdf
Remote file: https://huntabadday.com/docs/TSC-LWC31.pdf

### Examples:
###### Multiplication (Shift and add method)
```
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
```
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

## LWC 33
CustomASM assembler: https://github.com/hlorenzi/customasm
<br>
lwc33.asm is the instruction definition for CustomASM.
<br>
<br>
LWC33 programming and hardware manual: https://huntabadday.com/docs/TSC-LWC33.pdf
<br>
Machine architecture reference sheet: https://docs.google.com/spreadsheets/d/1v1xfn7EJRyUyvGW_dy4vbZQG2pKG-EaYQxawBvIN3sM/edit?usp=sharing

## 16x16 RAM
Pinout (Looking from top down):
```
AAAAAAAAAAAAAAAA EL
===================
DDDDDDDDDDDDDDDD RW
```
- A - Address (LSB on the right)
- D - Data I/O (LSB on the right), connect the outputs to the inputs for bidirectional data.
- E - Enable (Or chip select), must be active to read/write
- R - Read
- W - Write
- L - Load file

### Upload files to ram
To upload a file from your computer, into the RAM chip, run the `loadraml FILE` or `loadramh` commands in the console, where FILE is the path to the file to load.
<br>
The difference between the two is that one loads in low byte order, and the other loads in high byte order.

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

## Serial Transmitter and Receivers
Converters to transmit and receive serial data. Comes in 8 and 16 bit versions.

### Transmitter pinout
```
          S
===========F
DDDDDDDD WW

S - Serial output.
D - Data input.
WW - Two write signals, both must be active to write.
F - One tick pulse when finished transmitting.

Least significant bit of data is on the right.
```

### Receiver pinout
```
          S
===========
DDDDDDDD  F

S - Serial input.
D - Data output.
F - One tick pulse when data is received.

Least significant bit of data is on the right.
```

### Protocol
```
Data direction
==>
+--------+-+
|DDDDDDDD|S|
+--------+-+

Serial packet starts with one start bit, followed by the data, least significant bit first.
```



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
Each interval timer consists of a 16-bit read-only Timer Counter and a 16-bit write only Timer Latch. Data written to the timer are latched in the Timer Latch, while data read from the timer are the present contents of the Time Counter. The timers can be used independently or linked for extended operations. The various timer modes allow generation of time delays and variable frequency output. Utilizing the CNT input, the timers can count external pulses or measure frequency, pulse width and delay times of external signals. Each timer has an associated control register, providing independant control of the following functions:

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
On the front, in order from left to right is the data I/O (LSB Right), chip select / enable, read, write, RS (register select).\
On the right is a reset pin.\
16 bit versions of the chip will have a second pin close to the RS, this will enable 16 bit mode.

### Data Input
Writing to the chip while the RS pin is low will append data to the internal buffer. On the 16 bit versions of the chip, two bytes are appended to the internal buffer.

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
On the front, in order from left to right is the data I/O (LSB Right), chip select / enable, read, write, RS (register select).\
(Only for INT version) On the right is a reset pin and an interrupt output, this is on the INT version of the receiver and it is HIGH when a packet is available.\
16 bit versions of the chip will have a second pin close to the RS, this will enable 16 bit mode.

#### Data Output
Reading from the chip while the RS pin is low will read an octet from the internal buffer.\
The status pin will turn on when the buffer is empty while reading, this pin isn't strictly needed for operation since there is the "buffer empty" bit in the control register. On the 16 bit version of the chip, two bytes are read from the buffer and outputted to the IO port when in 16 bit mode, the status pin will turn on if only 1 byte is read instead of 2 when in 16 bit mode.

### Control Register
The control register can be accessed while the RS pin is high. Interrupts available on the second version of the chip.

#### Control Register (Read / Write)
- 0: Packet available / Start reading next packet
- 1: Is buffer empty / Clear packet stack & clear buffer
- 2: _ / Include checksum at the end of the data
- 3: _ / Enable interrupt
- 4: _ / Disable interrupt
- 5: Not used
- 6: Not used
- 7: Interrupt occured

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
- Output enable
- Selector bit 3
- Selector bit 2
- Selector bit 1
- Selector bit 0

There are versions of the plexers that have an output disable pin in place of the enable.

On the back are 16 pins for the multiplexed signal.

## TSC6540 Video Chip
The TSC6540 is a video chip which can be useful for driving a bitmap screen. This chip is capable of driving text, as well as displaying custom graphics.

### Pinout
#### Computer facing side (Side with input pegs) (From left to right):
- Data pins: D15 - D0
- Enable (To allow the device to read/write to the data bus)
- Read
- Write
- Address pins: A10 - A0 (Address pin A10 can virtually be used as a select to select between text memory and internal control registers)
- DE (While HIGH, draw to the screen)
- RST (Clears the internal draw buffer)

#### Screen facing side (Side with output pegs) (From left to right):
- Fill (While HIGH, set the whole screen to the selected colour)
- Set (Draw the colour to the selected X/Y location)
- Colour: C0 - C15
- X position: X0 - X7
- Y position: Y0 - Y7

### How to use:
#### Text buffer:
Reading / writing to the text buffer can be done when access the chip at addresses 0x000 - 0x3FF.

#### Internal control registers:
Reading / writing to the internal control registers can be done when accessing the chip at addresses 0x400 - 0x40F.
<br>
I.E.: 0x0 - 0xF with A10 HIGH.

#### Internal control register list:
```
0x0        : VRAM Address (Auto increment)
0x1        : VRAM Read/Write
0x2        : Screen Buffer Address (Auto increment)
0x3        : Screen Buffer Read/Write
0x4        : Resolution X (Set the display width)
0x5        : Resolution Y (Set the display height)
0x6        : Text Dimension X (Set the text window width)
0x7        : Text Dimension Y (Set the text window height)
0x8        : Text offset X (Set the text window offset from the left)
0x9        : Text offset Y (Set the text window offset from the top)
0xA        : Control register (Extra control register)
0xB        : Text scroll (Scroll text up by an amount)
0xC        : Graphic display Y/X (Bits 0-7: Graphics display X position; 8-15: Graphics display Y position)
0xD        : Graphic display vector (On write, display a graphic defined in VRAM at the specified address, at the specified location by register 0xC)
0xE        : Currently unused
0xF        : Character set location (Vector pointing to a custom character set) (0 to use default character set)
```

###### Extra control register:
A write to this register with the specified bit set to 1 will perform the specified action.
```
0: Clear all (Resets to the colour in the first slot; set to default character set; Sets all characters to spaces)
1: Clear text (Sets all characters to spaces)
2: Reset colour memory (Resets to the colour in the first slot)
3: 
4: 
5: 
6:  
7: Redraw (Redraws all of the text) (This really shouldn't be used)
```

#### VRAM
You can read and write to the internal VRAM memory by using control register 0x1, the address you read or write to can be set using control register 0x0. When reading or writing to VRAM, the address register is incremented automatically.

#### Internal VRAM memory map:
```
0x0000 - 0x0400: Text buffer (This is also accessed using normal writes to the chip from addresses 0x000 - 0x3FF)
0x0400 - 0x07FF: Text foreground colour (Defines the foreground colour of every character)
0x0800 - 0x0BFF: Text background colour (Defines the background colour of every character)
0x0C00 - 0x0C7F: Characters 0x80 to 0xff when using the default character set
0x0C80 - 0xFFFF: Free memory
```

#### Custom character sets
To use a custom character set, upload the character data to VRAM, and set control register 0xF to point to the start of this data.
A character set contains a list of characters, where each character is defined by 8, 8-bit bytes. Each byte represents one line of the character, each bit in the byte representing a pixel, an on bit is set to the foreground colour, and off bit is set to the background colour.
This makes every character exactly 8x8 pixels.

#### Displaying graphics
To display a graphic, you must upload a graphic to the chip, and make a graphic definition. The graphic is a list of colour values, each representing a single pixel.
<br>
A graphic definition is made of 3, 16 bit bytes:
```
0: Res X (Graphic width)
1: Res Y (Graphic height)
2: Data vector (Vector pointing to the start of the graphic data)
```

#### Getting started (Basic chip setup):
1. Set resolution to screen width and height.
2. Set text dimensions to fill the screen.
3. Set X and Y offset to 0.
4. Set character set vector to 0 (Use the default character set).
5. Set VRAM address 0x0400 to foreground colour for text.
6. Set VRAM address 0x0800 to background colour for text.
7. Write 0x0001 to control register 0xA to clear the text, and to set all of the colour memory.

## Terminal Controller
The terminal controller is used to make the text display from CheeseUtilMod into a terminal which supports escape sequences for actions such as moving the cursor.

### Pinout
- On the front is a grid of outputs which correspond directly to the inputs on the CheeseUtil text display.
- While facing the back of the component, on the rear face is the data input, least significant bit on the right. On the left side is a reset input, and a bell output for when a character of 0x07 is sent to the controller. On the right side is write pin to send the data to the controller.

### Normal operation
While in normal operation, sending text characters to the controller will print the character on the display, and advance the cursor forward.
Any non text characters are not printed.

### Control codes
```
0x07 (BEL) Bell, enables the BELL pin for one tick.
0x08 (BS) Backspace, Moves the cursor left (but may "backwards wrap" if cursor is at start of line), deleting the character before the current position.
0x0A (LF) Line Feed, Moves to the next line and returns the cursor to the left, scrolls the display up if at bottom of the screen.
0x0C (FF) Form Feed, Clears the screen and returns the cursor to the upper-left.
0x1B (ESC) Escape, starts all the escape sequences.
```

### Escape sequences
Currently, only CSI (Control Sequence Introducer) commands are supported.
A CSI command is started with an ESC character followed by a '[' character. A CSI sequence contains any amount of decimal number parameters separated by a ';', ended by a character in the range of 0x40 to 0x7E.
<br>
(See: https://en.wikipedia.org/wiki/ANSI_escape_code#CSIsection)
<br>
List of supported CSI sequences:
`CUU, CUD, CUF, CUB, CNL, CPL, CHA, CUP, ED, EL, SU, SD, HVP, SGR`
<br>
```
Implemented custom sequences:
CSI s (Save current cursor position)
CSI u (Restore saved cursor position)
```

### Select Graphics Rendition (SGR)
Supported SGR attributes:
```
3: Enable cursor blink
7: Enable reverse character printing
25: Disable cursor blink
27: Disable reverse character printing
```

### Setting terminal dimensions
The controller can be edited to bring up a GUI to set the terminal dimensions.

## TSC 3301
The TSC 3301 is a self contained computer based on the LWC33 CPU.

### Pinout
```
    BBBBBBBBBBBBBBBB W TTTT CC
SR0                  R         TNET Rx
ST0                            TNET Tx
             TSC 3301          Ld
SR1                            Rst
ST1                  R         Clk
    AAAAAAAAAAAAAAAA W XXXX QQ

SR0: Serial 0 Rx
ST0: Serial 0 Tx
SR1: Serial 1 Rx
ST1: Serial 1 Tx

A: Data port 0/A (Left-right: Bits 15-0)
B: Data port 1/B (Left-right: Bits 0-15)

R: Data port read output (1 tick pulse when cpu reads port)
W: Data port write output (1 tick pulse when cpu writes port)

T: Timer outputs (Left-right):
    Timer 1 timer B output
    Timer 1 timer A output
    Timer 0 timer B output
    Tiemr 0 timer A output

C: Timer CNT inputs (Left-right):
    Timer 1 CNT
    Timer 0 CNT
    
X: LWC33 AUX Flag Set (Left-right: LWC33 X0-X3)
Q: LWC33 IRQ Pins (Left-right: LWC33 Q3-Q4)

TNET Rx: TNET Rx
TNET Tx: TNET Tx
Ld: Enable ROM loading, load binary file using the "loadraml or loadramh" commands in the console.
Rst: Reset TSC 3301 and all components inside
Clk: Clock CPU
```

### Memory map
```
0000 - DFFF: RAM
E000 - FFFF: ROM
```

### Device map
```
Device Map:
0100 - 0107: Timer 0 (TSC 6530)
0108 - 010F: Timer 1 (TSC 6530)
0110 -     : TNET data
0111 -     : TNET 16-bit data
0112 -     : TNET control register
0114 -     : Buffer 0 R/W
0115 -     : Buffer 1 R/W
0116 -     : Buffer 2 R/W
0117 -     : Buffer 3 R/W
0118 -     : Buffer control
011A -     : Serial 0 data
011B -     : Serial 1 data
011C -     : Serial control
011D -     : GPIO 0
011E -     : GPIO 1
011F -     : Clear external interrupts

TNET Data: Read tnet receive buffer / write tnet send buffer
TNET 16-bit data: Read 16 bits from tnet receive buffer / write 16 bits to tnet send buffer
TNET control bits:
    (Read / Write)
    0: _ / Send packet
    1: Is write buffer empty / Clear write buffer
    2: Packet available / Read next packet
    3: Is read buffer empty / Clear read buffer and packet queue
    
Buffer 0-1 4K FIFO buffers: Read/write buffer
Buffer 2-3 4K LIFO buffers: Read/write buffer

Buffer control bits:
    (Read / Write)
    0: Buffer 0 data available / Buffer 0 clear
    1: Buffer 1 data available / Buffer 1 clear
    2: Buffer 2 data available / Buffer 2 clear
    3: Buffer 3 data available / Buffer 3 clear
    
Serial 0 data: Read / write serial port
Serial 1 data: Read / write serial port

Serial control bits:
    0: Serial 0 data available / Serial 0 clear receive buffer
    1: Serial 1 data available / Serial 1 clear receive buffer
    
GPIO0: Read/write port 0/A
GPIO1: Read/write port 1/B

Clear external interrupt bits:
    (Read / Write)
    0: _ / Clear IRQ3
    1: _ / Clear IRQ4
```

### Misc. info
```
Timer 0 / Timer 1 interrupt pins are both connected to LWC33 IRQ 0
TNET packet available interrupt is LWC33 IRQ 1

Programs loaded through the load commands are written to the 8K ROM at 0xE000
```