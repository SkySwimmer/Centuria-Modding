
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
int connect_socket(int d1, int d2, char* path, int sockDescr)
{
    // Setup
    struct sockaddr_un addr;
    addr.sun_family = AF_UNIX;
    strcpy(addr.sun_path, path);
    return connect(sockDescr, (struct sockaddr*) &addr, sizeof(addr));
}

// Close sockets
void socket_shutdown(int d1, int d2, int how, int socketDescr)
{
    shutdown(socketDescr, how);
}

// Send
int socket_send(int d1, int d2, void *__buf, int socketDescr, size_t __n, int __flags)
{
    return send(socketDescr, __buf, __n, __flags);
}

// Receive
int socket_recv(int d1, int d2, void *__buf, int socketDescr, size_t __n, int __flags)
{
    return recv(socketDescr, __buf, __n, __flags);
}
