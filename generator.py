## IGNORE THIS
## I used this to generate the input and output stuff for the component succ files


#                      0000000000000000
# 0000000000000000 00  1111111111111111  11 01 1

start_pos = -22
end_pos = start_pos-1
step = -1

file = open("genOut.txt", "w")

i = start_pos

while(i > end_pos):
    file.write("            -\n")
    #file.write(f"                length: 0.6\n")
    file.write(f"                position:\n")
    file.write(f"                    x: {i}\n")
    file.write(f"                    y: 0.5\n")
    file.write(f"                    z: 1.5\n")
    file.write(f"                rotation:\n")
    file.write(f"                    x: 90\n")
    file.write(f"                    y: 0\n")
    file.write(f"                    z: 0\n")
    
    i += step