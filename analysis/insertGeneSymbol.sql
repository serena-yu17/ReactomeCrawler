drop procedure if exists insertGenes;
delimiter //
create procedure insertGenes(id1 int, id2 int)
begin
	DECLARE symbol1 varchar(40);
	DECLARE symbol2 varchar(40);
	select symbol into symbol1 from geneid where id = id1 limit 1;
	select symbol into symbol2 from geneid where id = id2 limit 1;		
	if NOT exists (
		select * from interaction
		where gene1 = symbol1 
		and gene2 = symbol2 
		limit 1
		)
	then			
		insert into interaction values(symbol1, symbol2);
	end if;
end 
delimiter ;
;

drop procedure if exists insertID;
delimiter //
create procedure insertID(nid int, symbol varchar(40))
begin		
	if NOT exists (
		select * from geneid
		where id = nid 
		limit 1
		)
	then			
		insert into geneid values(nid, symbol);
	end if;
end 
delimiter ;
;
