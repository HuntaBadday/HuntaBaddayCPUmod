#subruledef reg {
	r0  => 1`4
	r1  => 2`4
	r2  => 3`4
	r3  => 4`4
	r4  => 5`4
	r5  => 6`4
	r6  => 7`4
	r7  => 8`4
	r8  => 9`4
	r9  => 10`4
	r10 => 11`4
	r11 => 12`4
	rr  => 13`4
	sp  => 14`4
	st  => 15`4
}

#subruledef subreg {
	INT0	=> 0`4
	INT1	=> 1`4
	INT2	=> 2`4
	INT3	=> 3`4
	INT4	=> 4`4
	BP		=> 5`4
	SM		=> 6`4
	VMV		=> 7`4
	VMS		=> 8`4
	VMSG	=> 9`4
	VMBP	=> 10`4
	VMSM	=> 11`4
	CS		=> 12`4
	DS		=> 13`4
	SPSWP	=> 14`4
	CTRL	=> 15`4
}

#ruledef {
	brk {i: u4}	=> 0x0 @ i @ 0x00
	
	; -- Jumping
	
	jmp {addr: u16}	=> 0x1 @ 0x0 @ 0b0000 @ 0b1000 @ addr
	jmp {op1: reg}	=> 0x1 @ op1 @ 0b0000 @ 0b1000
	
	jmp ({addr: u16})	=> 0x1 @ 0x0 @ 0b1000 @ 0b1000 @ addr
	jmp ({op1: reg})	=> 0x1 @ op1 @ 0b1000 @ 0b1000
	
	jmp short {addr: u16}	=> 0x1 @ 0x0 @ 0b0100 @ 0b1000 @ (addr-$-1)`16
	
	bcs {addr: u16}	=> 0x1 @ 0x0 @ 0b0100 @ 0b0001 @ (addr-$-1)`16
	bcc {addr: u16}	=> 0x1 @ 0x0 @ 0b0100 @ 0b1001 @ (addr-$-1)`16
	bcs {op1: reg}	=> 0x1 @ op1 @ 0b0100 @ 0b0001
	bcc {op1: reg}	=> 0x1 @ op1 @ 0b0100 @ 0b1001
	
	beq {addr: u16}	=> 0x1 @ 0x0 @ 0b0100 @ 0b0010 @ (addr-$-1)`16
	bne {addr: u16}	=> 0x1 @ 0x0 @ 0b0100 @ 0b1010 @ (addr-$-1)`16
	beq {op1: reg}	=> 0x1 @ op1 @ 0b0100 @ 0b0010
	bne {op1: reg}	=> 0x1 @ op1 @ 0b0100 @ 0b1010
	
	bmi {addr: u16}	=> 0x1 @ 0x0 @ 0b0100 @ 0b0100 @ (addr-$-1)`16
	bpl {addr: u16}	=> 0x1 @ 0x0 @ 0b0100 @ 0b1100 @ (addr-$-1)`16
	bmi {op1: reg}	=> 0x1 @ op1 @ 0b0100 @ 0b0100
	bpl {op1: reg}	=> 0x1 @ op1 @ 0b0100 @ 0b1100
	
	bxa {addr: u16}	=> 0x1 @ 0x0 @ 0b0101 @ 0b0000 @ (addr-$-1)`16
	bna {addr: u16}	=> 0x1 @ 0x0 @ 0b0101 @ 0b1000 @ (addr-$-1)`16
	bxa {op1: reg}	=> 0x1 @ op1 @ 0b0101 @ 0b0000
	bna {op1: reg}	=> 0x1 @ op1 @ 0b0101 @ 0b1000
	
	bxb {addr: u16}	=> 0x1 @ 0x0 @ 0b0110 @ 0b0000 @ (addr-$-1)`16
	bnb {addr: u16}	=> 0x1 @ 0x0 @ 0b0110 @ 0b1000 @ (addr-$-1)`16
	bxb {op1: reg}	=> 0x1 @ op1 @ 0b0110 @ 0b0000
	bnb {op1: reg}	=> 0x1 @ op1 @ 0b0110 @ 0b1000
	
	; -- Register copy
	
	mov {op1: reg}, {op2: reg}	=> 0x2 @ op1 @ op2 @ 0x0
	mov {op1: reg}, {data: i16}	=> 0x2 @ op1 @ 0x0 @ 0x0 @ data
	
	movs {op1: reg}, {op2: reg}	=> 0x2 @ op1 @ op2 @ 0x1
	movs {op1: reg}, {data: i16}	=> 0x2 @ op1 @ 0x0 @ 0x1 @ data
	
	; -- Store & load from memory
	
	mov {op1: reg}, [{op2: reg}] 	=> 0x3 @ op1 @ op2 @ 0x0
	mov {op1: reg}, [{addr: u16}]	=> 0x3 @ op1 @ 0x0 @ 0x0 @ addr
	mov {op1: reg}, [{op3: reg}+{offset: i16}]	=> 0x3 @ op1 @ 0x0 @ op3 @ offset
	mov {op1: reg}, [{op3: reg}-{offset: i16}]	=> 0x3 @ op1 @ 0x0 @ op3 @ -offset`16
	mov {op1: reg}, [{offset: i16}+{op3: reg}]	=> 0x3 @ op1 @ 0x0 @ op3 @ offset
	mov {op1: reg}, [{op2: reg}+{op3: reg}]	=> 0x3 @ op1 @ op2 @ op3
	
	mov [{op2: reg}], {op1: reg}	=> 0x4 @ op1 @ op2 @ 0x0
	mov [{op2: reg}], {data: i16}	=> 0x4 @ 0x0 @ op2 @ 0x0 @ data
	mov [{addr: u16}], {op1: reg}	=> 0x4 @ op1 @ 0x0 @ 0x0 @ addr
	mov [{op3: reg}+{offset: i16}], {op1: reg}	=> 0x4 @ op1 @ 0x0 @ op3 @ offset
	mov [{op3: reg}-{offset: i16}], {op1: reg}	=> 0x4 @ op1 @ 0x0 @ op3 @ -offset`16
	mov [{offset: i16}+{op3: reg}], {op1: reg}	=> 0x4 @ op1 @ 0x0 @ op3 @ offset
	mov [{op2: reg}+{op3: reg}], {op1: reg}	=> 0x4 @ op1 @ op2 @ op3
	mov [{op2: reg}+{op3: reg}], {data: i16}	=> 0x4 @ 0x0 @ op2 @ op3 @ data
	
	; ALU
	
	add {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x0
	add {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x0 @ data
	add {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x0 @ data
	
	adds {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x0
	adds {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x0 @ data
	adds {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x0 @ data
	
	adc {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x1
	adc {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x1 @ data
	adc {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x1 @ data
	
	adcs {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x1
	adcs {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x1 @ data
	adcs {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x1 @ data
	
	sub {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x2
	sub {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x2 @ data
	sub {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x2 @ data
	
	subs {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x2
	subs {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x2 @ data
	subs {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x2 @ data
	
	sbc {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x3
	sbc {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x3 @ data
	sbc {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x3 @ data
	
	sbcs {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x3
	sbcs {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x3 @ data
	sbcs {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x3 @ data
	
	and {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x4
	and {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x4 @ data
	and {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x4 @ data
	
	ands {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x4
	ands {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x4 @ data
	ands {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x4 @ data
	
	or {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x5
	or {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x5 @ data
	or {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x5 @ data
	
	ors {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x5
	ors {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x5 @ data
	ors {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x5 @ data
	
	xor {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x6
	xor {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x6 @ data
	xor {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x6 @ data
	
	xors {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x6
	xors {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x6 @ data
	xors {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x6 @ data
	
	rand {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x7
	rand {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x7 @ data
	
	rands {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x7
	rands {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x7 @ data
	
	shl {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x8
	shl {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x8 @ data
	shl {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x8 @ data
	
	shls {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x8
	shls {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x8 @ data
	shls {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x8 @ data
	
	rol {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0x9
	rol {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0x9 @ data
	rol {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0x9 @ data
	
	rols {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x9
	rols {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x9 @ data
	rols {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x9 @ data
	
	shr {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0xa
	shr {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0xa @ data
	shr {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0xa @ data
	
	shrs {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0xa
	shrs {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0xa @ data
	shrs {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0xa @ data
	
	ror {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0xb
	ror {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0xb @ data
	ror {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0xb @ data
	
	rors {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0xb
	rors {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0xb @ data
	rors {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0xb @ data
	
	mul {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0xc
	mul {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0xc @ data
	mul {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0xc @ data
	
	muls {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0xc
	muls {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0xc @ data
	muls {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0xc @ data
	
	smul {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0xd
	smul {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0xd @ data
	smul {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0xd @ data
	
	smuls {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0xd
	smuls {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0xd @ data
	smuls {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0xd @ data
	
	div {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0xe
	div {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0xe @ data
	div {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0xe @ data
	
	divs {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0xe
	divs {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0xe @ data
	divs {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0xe @ data
	
	sdiv {op1: reg}, {op2: reg}	=> 0x5 @ op1 @ op2 @ 0xf
	sdiv {op1: reg}, {data: i16}	=> 0x5 @ op1 @ 0x0 @ 0xf @ data
	sdiv {data: i16}, {op2: reg}	=> 0x5 @ 0x0 @ op2 @ 0xf @ data
	
	sdivs {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0xf
	sdivs {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0xf @ data
	sdivs {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0xf @ data
	
	; -- Same as subs
	cmp {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x2
	cmp {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x2 @ data
	cmp {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x2 @ data
	; -- Same as ands
	bit {op1: reg}, {op2: reg}	=> 0x6 @ op1 @ op2 @ 0x4
	bit {op1: reg}, {data: i16}	=> 0x6 @ op1 @ 0x0 @ 0x4 @ data
	bit {data: i16}, {op2: reg}	=> 0x6 @ 0x0 @ op2 @ 0x4 @ data
	
	; -- Subregisters
	
	mov {op1: reg}, {op2: subreg}	=> 0x7 @ op1 @ op2 @ 0x0
	mov {op2: subreg}, {op1: reg}	=> 0x7 @ op1 @ op2 @ 0x1
	mov {op2: subreg}, {data: i16}	=> 0x7 @ 0x0 @ op2 @ 0x1 @ data
	
	; -- Segment jump
	
	jps {op1: reg}, {seg: u4}	=> 0x8 @ op1 @ seg @ 0x0
	jps {addr: u16}, {seg: u4}	=> 0x8 @ 0x0 @ seg @ 0x0 @ addr
	
	jmp far {op1: reg}, {seg: u4}	=> 0x8 @ op1 @ seg @ 0x0
	jmp far {addr: u16}, {seg: u4}	=> 0x8 @ 0x0 @ seg @ 0x0 @ addr
	
	jvm {op1: reg}	=> 0x8 @ op1 @ 0x0 @ 0x1
	jvm {addr: u16}	=> 0x8 @ 0x0 @ 0x0 @ 0x1 @ addr
	
	jmp virt {op1: reg}	=> 0x8 @ op1 @ 0x0 @ 0x1
	jmp virt {addr: u16}	=> 0x8 @ 0x0 @ 0x0 @ 0x1 @ addr
	
	; -- Device I/O
	
	in {op1: reg}, {op2: reg}	=> 0x9 @ op1 @ op2 @ 0x0
	in {op1: reg}, {dev: u16}	=> 0x9 @ op1 @ 0x0 @ 0x0 @ dev
	
	out {op2: reg}, {op1: reg} 	=> 0x9 @ op1 @ op2 @ 0x1
	out {op2: reg}, {data: i16} 	=> 0x9 @ 0x0 @ op2 @ 0x1 @ data
	out {dev: u16}, {op1: reg}	=> 0x9 @ op1 @ 0x0 @ 0x1 @ dev
	
	; -- Stack
	
	push {op1: reg}	=> 0xa @ op1 @ 0x0 @ 0x0
	push {data: i16}	=> 0xa @ 0x0 @ 0x0 @ 0x0 @ data
	
	pop {op1: reg}	=> 0xb @ op1 @ 0x0 @ 0x0
	
	; -- Subroutines
	
	jsr {addr: u16}	=> 0xc @ 0x0 @ 0b0000 @ 0b1000 @ addr
	jsr {op1: reg}	=> 0xc @ op1 @ 0b0000 @ 0b1000
	
	jsr short {addr: u16}	=> 0xc @ 0x0 @ 0b0100 @ 0b1000 @ (addr-$-1)`16
	rjsr {addr: u16}	=> 0xc @ 0x0 @ 0b0100 @ 0b1000 @ (addr-$-1)`16
	
	scs {addr: u16}	=> 0xc @ 0x0 @ 0b0100 @ 0b0001 @ (addr-$-1)`16
	scc {op1: reg}	=> 0xc @ op1 @ 0b0100 @ 0b1001
	
	seq {addr: u16}	=> 0xc @ 0x0 @ 0b0100 @ 0b0010 @ (addr-$-1)`16
	sne {op1: reg}	=> 0xc @ op1 @ 0b0100 @ 0b1010
	
	smi {addr: u16}	=> 0xc @ 0x0 @ 0b0100 @ 0b0100 @ (addr-$-1)`16
	spl {op1: reg}	=> 0xc @ op1 @ 0b0100 @ 0b1100
	
	sxa {addr: u16}	=> 0xc @ 0x0 @ 0b0101 @ 0b0000 @ (addr-$-1)`16
	sna {op1: reg}	=> 0xc @ op1 @ 0b0101 @ 0b1000
	
	sxb {addr: u16}	=> 0xc @ 0x0 @ 0b0110 @ 0b0000 @ (addr-$-1)`16
	snb {op1: reg}	=> 0xc @ op1 @ 0b0110 @ 0b1000
	
	rts	=> 0xd000
	rti	=> 0xe000
	
	; -- Other
	
	nop	=> 0xf000
	
	; Flags
	
	sec	=> asm {or st, 0x0001}
	clc	=> asm {and st, !0x0001}
	
	sez	=> asm {or st, 0x0002}
	clz	=> asm {and st, !0x0002}
	
	sen	=> asm {or st, 0x0004}
	cln	=> asm {and st, !0x0004}
	
	sax0	=> asm {or st, 0x0010}
	clx0	=> asm {and st, !0x0010}
	
	sax1	=> asm {or st, 0x0020}
	clx1	=> asm {and st, !0x0020}
	
	sax2	=> asm {or st, 0x0040}
	clx2	=> asm {and st, !0x0040}
	
	sax3	=> asm {or st, 0x0080}
	clx3	=> asm {and st, !0x0080}
	
	sax	=> asm {or st, 0x00f0}
	clx	=> asm {and st, !0x00f0}
	
	sei1	=> asm {or st, 0x0100}
	cli1	=> asm {and st, !0x0100}
	
	sei2	=> asm {or st, 0x0200}
	cli2	=> asm {and st, !0x0200}
	
	sei3	=> asm {or st, 0x0400}
	cli3	=> asm {and st, !0x0400}
	
	sei4	=> asm {or st, 0x0800}
	cli4	=> asm {and st, !0x0800}
	
	sei	=> asm {or st, 0x0f00}
	cli	=> asm {and st, !0x0f00}
	
	sevm	=> asm {or st, 0x1000}
	clvm	=> asm {and st, !0x1000}
	
	saa	=> asm {or st, 0x2000}
	caa	=> asm {and st, !0x2000}
	
	seu	=> asm {or st, 0x4000}
	clu	=> asm {and st, !0x4000}
	
	; Other control
	
	backup	=> asm {mov CTRL, 0b1}
	restore	=> asm {mov CTRL, 0b10}
	restoregp	=> asm {mov CTRL, 0b100}
	restorest	=> asm {mov CTRL, 0b1000}
	
	; Pause
	pause	=> asm {
		clc
		bcc $
	}
}