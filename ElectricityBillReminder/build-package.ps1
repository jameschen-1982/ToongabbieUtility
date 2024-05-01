docker build lambda --tag toongabbieutility.electricitybillreminder --progress=plain

$id = docker create toongabbieutility.electricitybillreminder
docker cp ${id}:/lambda/ToongabbieUtility.ElectricityBillReminder.zip .
docker rm -v $id
