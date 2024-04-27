docker build lambda --tag rosterdutyreminder --progress=plain

$id = docker create rosterdutyreminder
docker cp ${id}:/lambda/RosterDutyReminder.zip .
docker rm -v $id
