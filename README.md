# Project Manager
This is a playground where I create the same basic application using different approaches.

## The application concept
The application is a simple project managing tool. It enables the user to create project,s and activities for the project, and manage the lifecycle of them.

The requirements for projects are:

* Projects have a name, an optional description, and a status which can be Pending, Active and Closed.
* Activities also have a name, an optional description, and a status which can be Pending, Active and Closed. They also have an assignee..
* When creating projects and activities a name must be supplied, and the status is automatically set to pending.
* Pending projects can be set to Active.
* Active projects can be set to Closed, but only if they do not have any activities that are not in the state Closed.
	* Active projects can be set to Pending, but only if they do not have any activities that are not in the state Pending.
* Closed projects can not be edited.
* Projects can be deleted. This also deletes activities tasks in the project.
* Activities can be added to Pending and Active projects.
* The activity assignee can be changed if the task is Pending or Active.
* Pending activities can be set to Active, but only if they have an assignee, and only on projects that are Active.
* Active activities can be set to Closed or Pending.
* Closed activities can be reactivated as long as the project is Active.
* When closed, activities can not be edited.
* Non-closed activities can be deleted. Closed activities can only be deleted if the project is deleted.
* Activities can be moved to other projcets, but only if activity and both projects are not closed.
  If the activity is active then the new project must also be active.
