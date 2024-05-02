docker build lambda --tag toongabbieutility.rentpaymentreminder --progress=plain

$id = docker create toongabbieutility.rentpaymentreminder
docker cp ${id}:/lambda/ToongabbieUtility.RentPaymentReminder.zip .
docker rm -v $id
