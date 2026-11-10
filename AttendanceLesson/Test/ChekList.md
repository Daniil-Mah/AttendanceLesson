# Чек-лист тестирования AttendanceLesson API

| №  | Действие | Ожидаемый результат | Реальный результат |
|----|----------|---------------------|-------------------|
| **Роли (Roles)** |
| 1 | POST `/roles/` с валидным именем (например, "newrole") | 201 Created | |
| 2 | POST `/roles/` с пустым именем | 400 Bad Request (валидация DTO) | |
| 3 | GET `/roles` с токеном администратора | 200 OK, список ролей | |
| 4 | GET `/roles` без токена | 401 Unauthorized | |
| 5 | GET `/roles/{id}` с существующим id | 200 OK, объект роли | |
| 6 | GET `/roles/{id}` с несуществующим id | 404 Not Found | |
| 7 | PATCH `/roles/{id}` с валидным именем | 204 No Content, имя обновлено | |
| 8 | DELETE `/roles/{id}` с существующим id | 204 No Content, роль удалена | |
| 9 | DELETE `/roles/{id}` с несуществующим id | 404 Not Found | |
| **Пользователи (Users)** |
| 10 | POST `/users/` с валидными данными (имя, пароль, роль, группа) | 201 Created | |
| 11 | POST `/users/` с пустым именем, коротким паролем, несуществующей ролью | 400 Bad Request | |
| 12 | GET `/users` с токеном администратора | 200 OK, список пользователей (Id, Name, Role) | |
| 13 | GET `/users/{id}` с существующим id | 200 OK, данные пользователя (Id, Name, Role, Group, IsArchived) | |
| 14 | GET `/users/{id}` с несуществующим id | 404 Not Found | |
| 15 | PATCH `/users/{id}` с валидными полями (Name, RoleId, GroupId) | 204 No Content, поля обновлены | |
| 16 | PATCH `/users/{id}` с несуществующим id | 404 Not Found | |
| 17 | DELETE `/users/{id}` с существующим id | 204 No Content, пользователь удалён | |
| 18 | DELETE `/users/{id}` с несуществующим id | 404 Not Found | |
| 19 | POST `/users/{id}archive` для активного пользователя | 204 No Content, IsArchive = true, ArchivedAt установлен | |
| 20 | POST `/users/{id}archive` для уже архивированного пользователя | 400 Bad Request | |
| 21 | POST `/users/set-password?newPassword=...` с валидным паролем | 204 No Content, пароль изменён | |
| 22 | POST `/users/set-password` для архивированного пользователя | 409 Conflict | |
| 23 | GET `/users/me` с валидным токеном | 204 No Content, в ответе Set-Cookie с userInfo | |
| 24 | GET `/users/me` без токена | 401 Unauthorized | |
| **Группы (Groups)** |
| 25 | POST `/groups/` с валидным именем (5–8 символов) | 201 Created | |
| 26 | POST `/groups/` с именем короче 5 символов | 400 Bad Request | |
| 27 | GET `/groups` с токеном администратора | 200 OK, список групп | |
| 28 | GET `/groups` без токена | 401 Unauthorized | |
| 29 | GET `/groups/{id}` с существующим id | 200 OK, объект группы | |
| 30 | GET `/groups/{id}` с несуществующим id | 404 Not Found | |
| 31 | PATCH `/groups/{id}` с валидным именем | 204 No Content, имя обновлено | |
| 32 | DELETE `/groups/{id}` с существующим id | 204 No Content, группа удалена | |
| 33 | DELETE `/groups/{id}` с несуществующим id | 404 Not Found | |
| **Уроки (Lessons)** |
| 34 | POST `/lessons/` с валидными данными (имя, TeacherId) | 201 Created | |
| 35 | POST `/lessons/` с пустым именем | 400 Bad Request | |
| 36 | POST `/lessons/` с несуществующим TeacherId | 400 Bad Request (FK) | |
| 37 | GET `/lessons` с токеном администратора | 200 OK, список уроков | |
| 38 | GET `/lessons` без токена | 401 Unauthorized | |
| 39 | GET `/lessons/{id}` с существующим id | 200 OK, объект урока | |
| 40 | GET `/lessons/{id}` с несуществующим id | 404 Not Found | |
| 41 | PATCH `/lessons/{id}` с валидным именем | 204 No Content, имя обновлено | |
| 42 | DELETE `/lessons/{id}` с существующим id | 204 No Content, урок удалён | |
| 43 | DELETE `/lessons/{id}` с несуществующим id | 404 Not Found | |
| **Журнал посещаемости (Logbooks)** |
| 44 | POST `/logbooks/` с валидными данными (UserId, GroupId, LessonId, AbsenceDuration >= 0, IsPresent) | 201 Created | |
| 45 | POST `/logbooks/` с отрицательным AbsenceDuration | 400 Bad Request (валидация) | |
| 46 | POST `/logbooks/` с IsPresent=false и AbsenceReason=null | 400 Bad Request (нарушение check constraint CK_Logbook_AbsenceReason) | |
| 47 | POST `/logbooks/` с несуществующим UserId/GroupId/LessonId | 400 Bad Request (FK) | |
| 48 | POST `/logbooks/` с дубликатом (тот же UserId, LessonId, дата CreatedAt) | 409 Conflict (уникальный индекс) | |
| 49 | GET `/logbooks` с токеном администратора | 200 OK, логи сгруппированы по группам | |
| 50 | GET `/logbooks` без токена | 401 Unauthorized | |
| 51 | GET `/logbooks/{groupId}` с существующим groupId | 200 OK, список записей группы | |
| 52 | PATCH `/logbooks/{id}` с валидными полями | 204 No Content, поля обновлены | |
| 53 | PATCH `/logbooks/{id}` с IsPresent=true | AbsenceReason сбрасывается в null | |
| 54 | PATCH `/logbooks/{id}` с несуществующим id | 404 Not Found | |
| 55 | DELETE `/logbooks/{id}` с существующим id | 204 No Content, запись удалена | |
| 56 | DELETE `/logbooks/{id}` с несуществующим id | 404 Not Found | |
| **Расписание (Schedules)** |
| 57 | POST `/schedules/` с валидными данными (GroupId, LessonId, Date, StartTime < EndTime) | 201 Created | |
| 58 | POST `/schedules/` с StartTime >= EndTime | 400 Bad Request (нарушение check constraint ck_time) | |
| 59 | POST `/schedules/` с несуществующим GroupId/LessonId | 400 Bad Request (FK) | |
| 60 | GET `/schedules` с токеном администратора | 200 OK, список расписаний с именами группы и урока | |
| 61 | GET `/schedules` без токена | 401 Unauthorized | |
| **Аутентификация и авторизация (Auth)** |
| 62 | POST `/login` с валидными учётными данными | 200 OK, accessToken в теле, refreshToken в Set-Cookie | |
| 63 | POST `/login` с неверным паролем | 401 Unauthorized | |
| 64 | POST `/login` с несуществующим пользователем | 401 Unauthorized | |
| 65 | POST `/refresh` с валидным refreshToken в cookie | 200 OK, новый accessToken, обновлённый refreshToken в cookie | |
| 66 | POST `/refresh` без refreshToken | 401 Unauthorized | |
| 67 | POST `/refresh` с просроченным refreshToken | 401 Unauthorized, refreshToken отозван | |
| 68 | Доступ к защищённым эндпоинтам с истёкшим accessToken (15 мин) | 401 Unauthorized | |
| 69 | Доступ к `/roles` с токеном роли "student" | 403 Forbidden | |
| 70 | Доступ к `/logbooks` с токеном роли "teacher" или "headman" | 200 OK | |
| **Валидация DTO** |
| 71 | CreateGroup с именем длиной 5–8 символов | Успешно | |
| 72 | CreateGroup с именем длиной 4 символа | Ошибка валидации (400) | |
| 73 | CreateLesson с именем длиной 3–20 символов | Успешно | |
| 74 | CreateLesson с именем длиной 2 символа | Ошибка валидации (400) | |
| 75 | CreateUser с паролем 8–50 символов | Успешно | |
| 76 | CreateUser с паролем 7 символов | Ошибка валидации (400) | |
| 77 | CreateLogBook с AbsenceDuration = TimeSpan.Zero | Успешно | |
| 78 | CreateLogBook с AbsenceDuration < 0 | Ошибка валидации (400) | |
| **Ограничения базы данных** |
| 79 | Вставка Logbook с IsPresent=false и AbsenceReason=null напрямую в БД | Исключение DbUpdateException (нарушение CK_Logbook_AbsenceReason) | |
| 80 | Вставка Schedule с StartTime >= EndTime напрямую в БД | Исключение DbUpdateException (нарушение ck_time) | |
| 81 | Вставка Role с дублирующимся именем | Исключение DbUpdateException (уникальный индекс) | |
| 82 | Вставка Group с дублирующимся именем | Исключение DbUpdateException (уникальный индекс) | |
| 83 | Вставка User с несуществующим RoleId | Исключение DbUpdateException (FK) | |
| 84 | Удаление Group, на которую ссылаются Users (SetNull) | Users.GroupId становится null | |
| 85 | Удаление Group, на которую ссылаются Logbooks (Restrict) | Исключение DbUpdateException | |
| 86 | Удаление Lesson, на которую ссылаются Logbooks (Restrict) | Исключение DbUpdateException | |
| **Обработчик исключений (ExceptionHandler)** |
| 87 | Ошибка PostgreSQL SQLSTATE 23502 | 400, "Обязательное поле не заполнено" | |
| 88 | Ошибка PostgreSQL SQLSTATE 23503 | 400, "Связанная запись не найдена" | |
| 89 | Ошибка PostgreSQL SQLSTATE 23505 | 409, "Запись уже существует" | |
| 90 | Ошибка PostgreSQL SQLSTATE 23514 | 400, "Нарушено ограничение" | |
| 91 | Неизвестная ошибка PostgreSQL | 500, "what" | |
| 92 | DbUpdateException с вложенным PostgresException | Код и сообщение извлекаются из внутреннего исключения | |
| **Фоновый сервис (SetAbsence)** |
| 93 | Запуск приложения | Фоновый сервис запускается, логирует "backgroud gone" каждые 15 минут | |
| 94 | Наличие завершённого занятия и отсутствие записи в Logbook для студента | Создаётся запись Logbook с IsPresent=false, AbsenceReason=null, AbsenceDuration = длительность занятия | |
| 95 | Повторный цикл при наличии записи | Дубликат не создаётся (проверка по UserId, LessonId, дате CreatedAt) | |
| **Безопасность и прочее** |
| 96 | Rate limiting: >100 запросов с одного ключа за 10 секунд | 429 Too Many Requests | |
| 97 | Cookie refreshToken: HttpOnly=false, Secure=false, SameSite=Strict, Path=/ | Установлены корректные атрибуты | |
| 98 | JWT: срок действия 15 минут, ClockSkew=0 | Токен отклоняется сразу после истечения | |
| 99 | Пароли хешируются (PasswordHasher) | В БД хранится хеш, а не открытый пароль | |
| 100 | Swagger UI доступен по `/swagger` | Отображается документация API с поддержкой Bearer | |
