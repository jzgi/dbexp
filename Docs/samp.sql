create database samp
  with owner postgres;

create type principal as
  (
  id varchar(11),
  inf varchar(11),
  credential varchar(32),
  addr varchar(30),
  wx varchar(28),
  posid smallint,
  created timestamp(6),
  admly smallint,
  orgly smallint,
  orgid smallint,
  status smallint
  );

alter type principal owner to postgres;

create table chats
(
  id      serial     not null
    constraint chats_pk
      primary key,
  hubid   varchar(2) not null,
  subject varchar(20),
  uname   varchar(254),
  posts   jsonb,
  posted  timestamp(6),
  fcount  smallint   not null,
  fname   varchar(10),
  img     bytea,
  status  smallint
);

alter table chats
  owner to postgres;

create table repays
(
  id     serial      not null,
  orgid  smallint,
  millid smallint,
  mgrid  varchar(11) not null,
  mgrwx  varchar(28),
  till   date        not null,
  orders integer,
  cash   money,
  paid   timestamp(6),
  payer  varchar(11),
  err    varchar(40),
  status smallint default 0
);

comment on table repays is '结算款项记录';

alter table repays
  owner to postgres;

create table items
(
  id      smallserial        not null
    constraint items_pk
      primary key,
  name    varchar(10)        not null,
  remark  varchar(100),
  icon    bytea,
  unit    varchar(4),
  price   money,
  ptcost  money,
  ctrcost money,
  status  smallint default 0 not null,
  img     bytea
);

comment on table items is '商品信息';

alter table items
  owner to postgres;

create table orgs
(
  id     smallserial           not null
    constraint orgs_pk
      primary key,
  name   varchar(10)           not null,
  addr   varchar(20),
  x      double precision,
  y      double precision,
  ispt   boolean  default true not null,
  isctr  boolean  default true not null,
  mgr    varchar(11),
  mgrwx  varchar(28),
  status smallint default 0    not null
);

comment on table orgs is '工坊以及客团信息';

alter table orgs
  owner to postgres;

create table ptctrs
(
  ptid   smallint           not null
    constraint ptctrs_ptid_fk
      references orgs,
  ctrid  smallint           not null
    constraint ptctrs_ctrid_fk
      references orgs,
  status smallint default 0 not null,
  constraint ptctrs_pk
    unique (ptid,
            ctrid)
);

comment on table ptctrs is '商品供应信息';

alter table ptctrs
  owner to postgres;

create table orderlns
(
  id        serial             not null
    constraint orderlns_pk
      primary key,
  custid    varchar(11)        not null,
  custinf   varchar(11)        not null,
  stationid smallint           not null,
  itemid    smallint           not null,
  shopid    smallint           not null,
  unit      varchar(4)         not null,
  price     money              not null,
  qty       smallint,
  total     money,
  created   timestamp(6),
  orderid   integer,
  status    smallint default 0 not null
);

alter table orderlns
  owner to postgres;

create index orders_custid
  on orderlns (custid);

create table ctritems
(
  ctrid  smallint,
  itemid smallint,
  cap    integer,
  min    integer,
  max    integer,
  status smallint
);

alter table ctritems
  owner to postgres;

create table users
(
  id         varchar(11)            not null
    constraint users_pkey
      primary key,
  inf        varchar(11)            not null,
  credential varchar(32),
  addr       varchar(30),
  wx         varchar(28),
  posid      smallint,
  created    timestamp(6) default ('now'::text)::timestamp without time zone,
  admly      smallint     default 0 not null,
  orgly      smallint     default 0 not null,
  orgid      smallint,
  status     smallint     default 0 not null
);

alter table users
  owner to postgres;

create view pt_items as
SELECT a.id,
       a.name,
       a.remark,
       a.unit,
       a.price,
       sum(
           b.cap) AS cap,
       c.ptid
FROM items a,
     ctritems b,
     ptctrs c
WHERE ((a.id =
        b.itemid) AND
       (b.ctrid =
        c.ctrid))
GROUP BY c.ptid,
         a.id;

alter table pt_items
  owner to postgres;

create view my_user as
SELECT users.id,
       users.inf,
       users.credential,
       users.addr,
       users.wx,
       users.posid,
       users.created,
       users.admly,
       users.orgly,
       users.orgid,
       users.status
FROM users
WHERE ((users.id)::text =
       current_setting(
           'principal.var'::text));

alter table my_user
  owner to postgres;

create view org_users as
SELECT users.id,
       users.inf,
       users.credential,
       users.addr,
       users.wx,
       users.posid,
       users.created,
       users.admly,
       users.orgly,
       users.orgid,
       users.status
FROM users
WHERE (users.orgid =
       (current_setting(
           'role.var'::text))::smallint);

alter table org_users
  owner to postgres;

create view all_orgs as
SELECT orgs.id,
       orgs.name,
       orgs.addr,
       orgs.x,
       orgs.y,
       orgs.ispt,
       orgs.isctr,
       orgs.mgr,
       orgs.mgrwx,
       orgs.status
FROM orgs;

alter table all_orgs
  owner to postgres;

create function get_item_icon(id smallint) returns bytea
  stable
  strict
  language sql
as
$$
select icon
from items
where
    id =
    id;
$$;

alter function get_item_icon(smallint) owner to postgres;

create function get_item_img(id smallint) returns bytea
  stable
  strict
  language sql
as
$$
select img
from items
where
    id =
    id;
$$;

alter function get_item_img(smallint) owner to postgres;

