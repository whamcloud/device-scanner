FROM rust:1.39 as builder
WORKDIR /build
COPY . .
RUN cargo build -p device-aggregator --release

FROM rust:1.39
COPY --from=builder /build/target/release/device-aggregator /usr/local/bin
RUN rm -rf /var/lib/apt/lists/*

CMD ["device-aggregator"]