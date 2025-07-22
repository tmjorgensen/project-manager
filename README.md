# Project Manager
This is a playground where I create the same basic application using different approaches.

## The application
The application is a simple project managing tool. It enables the user to create projects and tasks, and manage the lifecycle of them.

The requirements for projects are:

* Projects have a name, an optional description, and a status which can be Pending, Active and Closed.
* Tasks also have a name, an optional description, and a status which can be Pending, Active and Closed. They also have an asignee.
* When creating projects and tasks a name must be supplied, and the status is automatically set to pending.
* Pending projects can be set to Active.
* Active projects can be set to Closed, but only if they do not have any tasks that are not in the state Closed.
* Active projects can be set to Pending, ut only if they do not have any tasks that are not in the state Pending.
* Closed projects can not be edited.
* Projects can be deleted. This also deletes all tasks in the project.
* Tasks can be added to Pending and Active projects.
* The task assignee can be changed if the task is Pending or Active.
* Pending tasks can be set to Active, but only if they have an assignee, and only on projects that are Active.
* Active tasks can be set to Closed or Pending.
* Closed tasks can be reactivated as long as the project is Active.
* When closed, tasks can not be edited.
* Non-closed tasks can be deleted. Closed tasks can only be deleted if the project is deleted.
