# multiftp
A simple async SFTP client using SSH.NET

## Introduction
This is only a small project I made. I worked with SFTP at job and I found the work slow. It is a background process so it doesn't matter, but while talking with someone, he game me the idea to make this: not a multithreaded SFTP client, but a completely async one.

## The idea
You provide how many max FTP clients you want, e.g 4. You will get one "master" client who will read the directories and will dispatch corresponding files to "slave" clients. All of this done with the most asynchronicity as logically possible. The goal is to wait as little as possible to the FTP protocol, which is quite slow sometimes.

As I said, this is only a small project, I cannot garantee anything with it, but hey, it you test it properly and it works well in the happy paths and error paths, good for you! 


