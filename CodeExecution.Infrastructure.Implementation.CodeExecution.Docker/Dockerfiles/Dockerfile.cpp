FROM gcc:12

RUN apt-get update && \
    apt-get install -y time && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*