# HuntaBaddayCPUmod

Please ping or message me (HuntaBadday#7114 on discord) for ANY questions you have.

### ToDo:

Code comments.

## LWC 3.1

### Helpful Tools:
Instruction Documentation: https://docs.google.com/spreadsheets/d/15QtVbXtz8gos2C4d7pLwn_2SpXMISwSytKTilIrm0DM/edit#gid=0

CustomASM assembler: https://github.com/hlorenzi/customasm\
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

#### Examples:
###### Multiplication (Shift and add method)
```x86
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
init:
    ; Initialize interrupts and stack
    mov sp, 0xffff
    int irq ; Set interrupt location
    cli     ; Enable interupts
    
    ; Some code that loops to show the interrupts working
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
            ; So when the interrupt returns, the code that was running doesn't see any changes
    
    ; Shift the value in memory right
    mov r1, [0x8000]
    ror r1
    mov [0x8000], r1
        
    popall  ; Restore the registers
    rti

#addr 0xffff
#d16 init   ; Start vector pointing to init