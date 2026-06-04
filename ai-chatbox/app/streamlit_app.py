from __future__ import annotations

import os

import requests
import streamlit as st


API_URL = os.getenv("AI_CHATBOX_API_URL", "http://127.0.0.1:8001/chat")

st.set_page_config(page_title="IncuSmart AI Chatbox", layout="wide")
st.title("IncuSmart AI Chatbox")
st.caption("knowledge bằng RAG, recommend bằng ML + rule")

if "messages" not in st.session_state:
    st.session_state.messages = []

with st.sidebar:
    st.header("Context")
    ambient_temperature = st.number_input("Ambient temperature", value=30.0)
    ambient_humidity = st.number_input("Ambient humidity", value=65.0)
    notes = st.text_area("Notes", value="")

for message in st.session_state.messages:
    with st.chat_message(message["role"]):
        st.markdown(message["content"])
        if message.get("table"):
            st.dataframe(message["table"], use_container_width=True)
        if message.get("sources"):
            st.caption("Sources")
            for source in message["sources"]:
                st.write(f"- {source['source']}: {source.get('excerpt', '')}")

prompt = st.chat_input("Hỏi kiến thức hoặc yêu cầu recommend cấu hình...")
if prompt:
    st.session_state.messages.append({"role": "user", "content": prompt})
    with st.chat_message("user"):
        st.markdown(prompt)

    payload = {
        "message": prompt,
        "session_id": "streamlit-demo",
        "user_context": {
            "ambient_temperature": ambient_temperature,
            "ambient_humidity": ambient_humidity,
            "notes": notes,
        },
    }
    response = requests.post(API_URL, json=payload, timeout=120)
    response.raise_for_status()
    data = response.json()

    table = None
    if data.get("recommended_config"):
        rows = []
        for phase in data["recommended_config"]:
            for parameter in phase["parameters"]:
                rows.append(
                    {
                        "phase": phase["phase_name"],
                        "days": f"{phase['day_start']}-{phase['day_end']}",
                        "config_code": parameter["config_code"],
                        "config_name": parameter["config_name"],
                        "target": parameter["target_value"],
                        "min": parameter["min_value"],
                        "max": parameter["max_value"],
                        "unit": parameter["unit"],
                    }
                )
        table = rows

    assistant_message = {
        "role": "assistant",
        "content": data["answer"],
        "table": table,
        "sources": data.get("sources", []),
    }
    st.session_state.messages.append(assistant_message)
    with st.chat_message("assistant"):
        st.markdown(data["answer"])
        if table:
            st.dataframe(table, use_container_width=True)
        if data.get("sources"):
            st.caption("Sources")
            for source in data["sources"]:
                st.write(f"- {source['source']}: {source.get('excerpt', '')}")
