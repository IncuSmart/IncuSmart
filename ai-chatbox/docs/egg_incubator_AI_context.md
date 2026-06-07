# Project: Hệ thống AI cho máy ấp trứng

## Mục tiêu
Build hệ thống AI hỗ trợ vận hành máy ấp trứng gia cầm, gồm 3 chức năng chính qua chat interface:
1. **Hỏi đáp** data cũ bằng ngôn ngữ tự nhiên
2. **Nhập liệu** kết quả mẻ ấp qua chat
3. **Recommend** thông số cho mẻ ấp mới

---

## Stack kỹ thuật
- **VPS**: đã có sẵn
- **Database**: PostgreSQL (tự host trên VPS)
- **ML Model**: scikit-learn (Random Forest) — chạy local trên VPS
- **Chat interface**: Streamlit
- **LLM**: Gemini API (free tier — 1500 requests/ngày)
- **RAG**: sentence-transformers + ChromaDB
- **Text-to-SQL**: Gemini API
- **Ngôn ngữ**: Python
- **Chi phí**: $0

---

## Schema database

### Bảng `season` (mùa ấp lớn)
```
id
name               -- tên mùa ấp
start_date
end_date
total_eggs_in      -- tổng trứng đầu vào
total_eggs_hatched -- tổng trứng nở
success_rate       -- % thành công (tính tự động)
notes
created_at
```

### Bảng `phase` (giai đoạn trong mùa)
```
id
season_id          -- FK → season
phase_number       -- 1, 2, hoặc 3
day_start          -- ngày bắt đầu (1, 8, 19)
day_end            -- ngày kết thúc (7, 18, 21)
temp_set           -- nhiệt độ cài đặt (°C)
temp_actual_avg    -- nhiệt độ thực tế trung bình
humidity_set       -- độ ẩm cài đặt (%)
humidity_actual_avg-- độ ẩm thực tế trung bình
fan_speed          -- tốc độ quạt
turning_times_day  -- số lần đảo trứng/ngày
notes
```

### Bảng `candle_check` (soi trứng)
```
id
season_id          -- FK → season
check_day          -- ngày soi (7 hoặc 18)
eggs_removed       -- số trứng loại ra (chết phôi/yếu)
notes
```

---

## 3 giai đoạn ấp trứng (theo tài liệu FAO + chuẩn kỹ thuật)

| Phase | Ngày | Nhiệt độ | Độ ẩm |
|---|---|---|---|
| 1 | 1-7 | 37.5 - 37.8°C | 55-65% |
| 2 | 8-18 | 37.4 - 37.6°C | 55-65% |
| 3 (nở) | 19-21 | 37.2°C | 65-75% |

---

## AI Components

### ① ML Model (scikit-learn Random Forest)
- **Input**: thông số các phase (nhiệt độ, độ ẩm, quạt, đảo trứng...)
- **Output**: % thành công dự đoán
- **Note**: ~100 records, chạy CPU, rất nhẹ

### ② Text-to-SQL (Gemini API)
- User hỏi tiếng Việt → Gemini chuyển thành SQL → query PostgreSQL → trả kết quả
- Xử lý ở Gemini server, VPS không chịu tải

### ③ RAG (sentence-transformers + ChromaDB)
- Embed tài liệu kỹ thuật 1 lần, lưu ChromaDB local
- Mỗi query: tìm chunk liên quan → ghép prompt → Gemini trả lời
- RAM cần: ~550MB

### ④ Recommend Engine (Python thuần)
- Dựa trên: top mẻ thành công cao nhất + thời tiết hiện tại
- Output: bộ thông số gợi ý cho mẻ mới

---

## RAG Documents (đã tìm sẵn)
1. **FAO Tiếng Việt** — Thực hành tốt trong cơ sở ấp trứng:
   https://openknowledge.fao.org/server/api/core/bitstreams/e01000b5-5efc-4b03-829f-56a487b433e8/content
2. **FAO Tiếng Anh** — Management & Biosecurity in Hatcheries:
   https://internationalpoultrycouncil.org/wp-content/uploads/2020/10/FAO-Management-practices-and-biosecurity-in-hatcheries.pdf
3. **Mississippi State** — Hatchery Management Guide:
   https://www.poultry.msstate.edu/pdf/extension/hatchery_management_guide.pdf

---

## Intent Detection (phân loại câu hỏi)
Chat interface cần phân biệt 4 loại intent:
1. `query_data` → Text-to-SQL
2. `input_data` → Nhập kết quả mẻ vào DB
3. `recommend` → Recommend Engine + ML Model
4. `knowledge` → RAG tài liệu kỹ thuật

---

## Thứ tự build đề xuất
1. Schema + PostgreSQL setup
2. Nhập data mẫu
3. ML Model cơ bản
4. RAG pipeline (embed tài liệu)
5. Text-to-SQL
6. Intent detection
7. Chat interface (Streamlit)
8. Recommend engine
9. Ghép tất cả lại

---

## Lưu ý
- Backup PostgreSQL hàng ngày qua cronjob → Google Drive
- VPS cần ít nhất 1GB RAM
- Gemini API key: cần tạo tại https://aistudio.google.com
- Data nhập tay (không có sensor tự động)
- Loại trứng: chưa xác định (cần hỏi người vận hành)
