# Запросы (PostgreSQL)

* Схема и описание БД в **db-description.md**

## Задание 1: 100 заданий с самым долгим временем выполнения
Время, затраченное на выполнение задания - это период времени, прошедший с момента перехода задания в статус "В работе" и до перехода в статус "Выполнено".
Нужно вывести 100 заданий с самым долгим временем выполнения. 
Полученный список заданий должен быть отсортирован от заданий с наибольшим временем выполнения к заданиям с наименьшим временем выполнения.

Замечания:
- Невыполненные задания (не дошедшие до статуса "Выполнено") не учитываются.
- Когда исполнитель берет задание в работу, оно переходит в статус "В работе" (InProgress) и находится там до завершения работы. После чего переходит в статус "Выполнено" (Done).
  В любой момент времени задание может быть безвозвратно отменено - в этом случае оно перейдет в статус "Отменено" (Canceled).
- Нет разницы выполняется задание или подзадание.
- Выборка должна включать задания за все время.

Выборка должна содержать следующий набор полей:
- номер задания (task_number)
- заголовок задания (task_title)
- название статуса задания (status_name)
- email автора задания (author_email)
- email текущего исполнителя (assignee_email)
- дата и время создания задания (created_at)
- дата и время первого перехода в статус В работе (in_progress_at)
- дата и время выполнения задания (completed_at)
- количество дней, часов, минут и секнуд, которые задание находилось в работе - в формате "dd HH:mm:ss" (work_duration)

### Решение
```sql
with at_in_progress_first_tasks
    as (with at_in_progress_tasks
            as (select *
                     , row_number() over (partition by tl.task_id order by tl.at)
                         as in_progress_change_time
                  from task_logs tl
                 where tl.status = 3 /* InProgress */)
        select ip.task_id as task_id
             , ip.at      as time
          from at_in_progress_tasks ip
         where in_progress_change_time = 1)
, completed_tasks
    as (select tl.task_id as task_id
             , tl.at as time
          from task_logs tl
         where tl.status = 4 /* Done */)
select t.number as task_number
     , t.title as task_title
     , ts.name as status_name
     , creators.email as author_email
     , assignees.email as assignee_email
     , to_char(t.created_at at time zone 'UTC', 'DD.MM.YYYY HH24:MI:SS')
         as created_at
     , to_char(pt.time at time zone 'UTC', 'DD.MM.YYYY HH24:MI:SS')
         as in_progress_at
     , to_char(ct.time at time zone 'UTC', 'DD.MM.YYYY HH24:MI:SS')
         as completed_at
     , to_char(ct.time - pt.time, 'DD HH24:MI:SS') as work_duration
  from completed_tasks ct
  join at_in_progress_first_tasks pt on pt.task_id = ct.task_id
  join tasks t on t.id = ct.task_id
  join task_statuses ts on ts.id = t.status
  join users creators on creators.id = t.created_by_user_id
  join users assignees on assignees.id = t.assigned_to_user_id
 order by work_duration desc
 limit 100;
```

## Задание 2: Выборка для проверки вложенности
Задания могу быть простыми и составными. Составное задание содержит в себе дочерние - так получается иерархия заданий.
Глубина иерархии ограничено Н-уровнями, поэтому перед добавлением подзадачи к текущей задачи нужно понять, может ли пользователь добавить задачу уровнем ниже текущего или нет. Для этого нужно написать выборку для метода проверки перед добавлением подзадания, которая бы вернула уровень вложенности указанного задания и полный путь до него от родительского задания.

Замечания:
- ИД проверяемого задания передаем в sql как параметр _:parent_task_id_
- если задание _Е_ находится на 5м уровне, то путь должен быть "_//A/B/C/D/E_".

Выборка должна содержать:
- только 1 строку
- поле "Уровень задания" (level) - уровень указанного в параметре задания
- поле "Путь" (path)

### Решение
```sql
with recursive nesting_selection
    as (select t.id
             , t.parent_task_id
             , t.id::text as path
             , 1 as task_level
          from tasks t
         where id = :parent_task_id
         union all
        select t.id
             , t.parent_task_id
             , t.id::text || '/' || ns.path as path
             , ns.task_level + 1 as task_level
          from nesting_selection ns
          join tasks t on t.id = ns.parent_task_id)
select ns.task_level as level
     , '//' || ns.path as path
  from nesting_selection ns
 order by ns.task_level desc
 limit 1;
```

## Задание 3: Денормализация
Наш трекер задач пользуется популярностью и количество только активных задач перевалило уже за несколько миллионов. Продакт пришел с очередной юзер-стори:
```
Я как Диспетчер в списке активных задач всегда должен видеть задачу самого высокого уровня из цепочки отдельным полем

Требования:
1. Список активных задач включает в себя задачи со статусом "В работе"
2. Список должен быть отсортирован от самой новой задачи к самой старой
3. В списке должны быть поля:
  - Номер задачи (task_number)
  - Заголовок (task_title)
  - Номер родительской задачи (parent_task_number)
  - Заголовок родительской задачи (parent_task_title)
  - Номер самой первой задачи из цепочки (root_task_number)
  - Заголовок самой первой задачи из цепочки (root_task_title)
  - Email, на кого назначена задача (assigned_to_email)
  - Когда задача была создана (created_at)
 4. Должна быть возможность получить данные с пагинацией по N-строк (@limit, @offset)
```

Обсудив требования с лидом тебе прилетели 2 задачи:
1. Данных очень много и нужно денормализовать таблицу tasks
   Добавить в таблицу tasks поле `root_task_id bigint not null`
   Написать скрипт заполнения нового поля root_task_id для всей таблицы (если задача является рутом, то ее id должен совпадать с root_task_id)
2. Написать запрос получения данных для отображения в списке активных задач
   (!) Выяснилось, что дополнительно еще нужно возвращать идентификаторы задач, чтобы фронтенд мог сделать ссылки на них (т.е. добавить поля task_id, parent_task_id, root_task_id)

### Скрипты миграций
```sql
alter table tasks
    add column if not exists root_task_id bigint;

with root_tasks
    as (with recursive all_task_and_parents
        as (select t.id                             as task_id
                 , coalesce(t.parent_task_id, t.id) as root_task_id
                 , 1                                as level
              from tasks t
             union all
            select a.task_id        as task_id
                 , t.parent_task_id as root_task_id
                 , a.level + 1      as level
              from all_task_and_parents a
              join tasks t on t.id = a.root_task_id
             where t.parent_task_id is not null)
        , ranked_tasks_and_parents
            as (select *
                     , row_number() over (partition by a.task_id order by a.level desc) as row_number
                  from all_task_and_parents a)
        select r.task_id      as task_id
             , r.root_task_id as root_task_id
          from ranked_tasks_and_parents r
         where row_number = 1)
update tasks t
   set root_task_id = r.root_task_id
  from root_tasks r
 where r.task_id = t.id
returning r.root_task_id;

alter table tasks
    alter column root_task_id set not null;
```

### Запрос выборки
```sql
   select t.id as task_id
        , t.number as task_number
        , t.title as task_title
        , t.parent_task_id as parent_task_id
        , parent_tasks.number as parent_task_number
        , parent_tasks.title as parent_task_title
        , t.root_task_id as root_task_id
        , root_tasks.number as root_task_number
        , root_tasks.title as root_task_title
        , u.email as assigned_to_email
        , to_char(t.created_at at time zone 'UTC', 'DD.MM.YYYY HH24:MI:SS')
            as created_at
     from tasks t
left join tasks parent_tasks on parent_tasks.id = t.parent_task_id
     join tasks root_tasks on root_tasks.id = t.root_task_id
     join users u on u.id = t.assigned_to_user_id
    where t.status = 3 /* InProgress */
    order by t.created_at desc
    limit @limit
   offset @offset;
```
