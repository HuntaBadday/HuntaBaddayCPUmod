## IGNORE THIS
## I used this to generate the input and output stuff for the component succ files


start_pos = 2
end_pos = start_pos + 16

step = 1

file = open("genOut.txt", "w")
for j in range(0, 8, 1):
    for i in range(start_pos, end_pos, step):
        file.write("            -\n")
        #file.write(f"                length: 0.6\n")
        file.write(f"                position:\n")
        file.write(f"                    x: {i}\n")
        file.write(f"                    y: {j}.5\n")
        file.write(f"                    z: -0.5\n")
        file.write(f"                rotation:\n")
        file.write(f"                    x: -90\n")
        file.write(f"                    y: 0\n")
        file.write(f"                    z: 0\n")
file.close()