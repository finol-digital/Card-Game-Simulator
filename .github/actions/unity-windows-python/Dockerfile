ARG BASE_IMAGE
FROM ${BASE_IMAGE}
COPY python/ C:/Python/
ENV CGS_PYTHON=C:/Python/python.exe
ENV PYTHONIOENCODING=utf-8
RUN ["C:/Python/python.exe", "--version"]
