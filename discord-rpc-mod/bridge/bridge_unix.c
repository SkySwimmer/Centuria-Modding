
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

// Socket creation
int create_socket()
{
    // Create
    return socket(AF_UNIX, SOCK_STREAM, 0);
}

// Socket connection
int connect_socket(int sockDescr, char* path)
{    
    // Setup
    struct sockaddr_un addr;
    addr.sun_family = AF_UNIX;
    strcpy(addr.sun_path, path);
    FILE *f = fopen("test", "w");
    fputs(addr.sun_path, f);
    fclose(f);
    return connect(sockDescr, (struct sockaddr*) &addr, sizeof(addr));
}

// Close sockets
void socket_shutdown(int socketDescr, int how)
{
    shutdown(socketDescr, how);
}

// Send
int socket_send(int socketDescr, void *__buf, size_t __n, int __flags)
{
    return send(socketDescr, __buf, __n, __flags);
}

// Receive
int socket_recv(int socketDescr, void *__buf, size_t __n, int __flags)
{
    return recv(socketDescr, __buf, __n, __flags);
}
