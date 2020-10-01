# prp-be

Precision Reporters Platform BE


## Table of Contents

1. [PR Process](#pr-process)
1. [Branching Strategy](#branching-strategy)
1. [GIT Conventions](#git-conventions)
1. [Third-Party systems used](#third-party-systems-used)
1. [App setup / Test](#appsetup-test)


## PR Process

### Create a Branch
Whenever you begin work on a new feature or bugfix, it's important that you create a new branch. Not only is it proper git workflow, but it also keeps your changes organized and separated from the main branch so that you can easily submit and manage multiple pull requests for every task you complete.

To create a new branch and start working on it:

```shell
# Checkout the main branch - you want your new branch to come from main
git checkout main

# Create a new branch named newfeature (give your branch its own simple informative name)
git branch newfeature

# Switch to your new branch
git checkout newfeature
```

Now, go to town hacking away and making whatever changes you want to.

### Submitting a Pull Request

#### Cleaning Up Your Work

Prior to submitting your pull request, you might want to do a few things to clean up your branch and make it as simple as possible for the original repo's maintainer to test, accept, and merge your work.

If any commits have been made to the upstream main branch, you should rebase your development branch so that merging it will be a simple fast-forward that won't require any conflict resolution work.

```shell
# Fetch upstream main and merge with your repo's main branch
git fetch upstream
git checkout main
git merge upstream/main

# If there were any new commits, rebase your development branch
git checkout newfeature
git rebase main
```

#### Submitting

Once you've committed and pushed all of your changes to GitHub click the pull request button. If you need to make any adjustments to your pull request, just push the updates to GitHub. Your pull request will automatically track the changes on your development branch and update.

### Accepting and Merging a Pull Request

We will use Pull Requests in an effort to keep the quality of our code as high as we can. It will be important that the whole team compromises with the code quality and code reviewing is an important part of it. Two important mentions in this section

2. We encourage every single team member to take ownership of code quality, step in and take Pull Requests to be reviewed.
2. We have our code workflow protected in a way where two approvers will be required to push code into the repository. Make sure Team Lead / Architect is included in complex code reviews.
2. If you select individual reviewers, make sure you choose someone that will add value to the review and not someone that will blindly accept the change.


#### Automatically Merging a Pull Request
In cases where the merge would be a simple fast-forward, you can automatically do the merge by just clicking the button on the pull request page on GitHub.

Now that you're done with the feature branch, you're free to delete it.

```shell
git branch -d newfeature
```

## Branching Strategy

### Branch names

**.** Use lowercase and dash for word separation

Bad examples:

- `fixHTTPHeaders`
- `fix_http_headers`

Good examples:

- `fix-http-headers`

**.** Avoid strange characters.

Avoid characters that could be troublesome to command interpreters at all costs (`"`, `'`, `\`, `&`, `*`, `(`, `)`, `[`, `]`, `{`, `}`).

Bad examples:

- `add-*-as-wildcard-support` (so many people are going to hate you for this one)
- `add wildcard support` (spaces)

Good examples:

- `add-asterisk-to-wildcard-support`

**.** Do NOT include your username or the current date in the branch name.

That information is already present in the commit itself.

Bad examples:

- `alphagit/20170303/fix-my-bug`

Good examples:

- `fix-my-bug`

**.** Include the ticket identifier in the branch name

Use the ticket identifier from your project tracking system to help identify what problem is this codebase trying to solve.

Bad examples:

- `fix-my-bug`

Good examples:

- `CFP-11-fix-memory-leak`

### Tag names

**.** Use `vM.m` and [semver](http://semver.org/) as your standard.

You can extend it to `vM.m.b`, `vM.m.b.p`, `vM.m.b.p-notes` as you see fit, but at the very least, stick to the `vM.m` (major, minor version) as a start.

If you're building an environment that is naturally versioned with four numbers (like .NET builds), then use four numbers instead of three. If you're building in an environment with does not have a natural versioning system, stick to [semver](http://semver.org/).

Bad examples:

- `version-0.1`
- `0.1`

Good examples:

- `v1.0`
- `v1.0.1`
- `v1.0.0-beta`

### Special branches and branching strategy

**.** Keep the following branches with special meaning:

- `develop`: main development branch. Code should reach this branch after being reviewed by the proper approvers. This is where new features branches start from.
- `release`: the release candidate for your next upcoming release. If you have multiple releases happening simultaneously, feel free to have several `release-xxx` branches. (Replace `xxx` with a description that suits you.) This is where bugfixing branches for the release start from.
- `main`: the latest version of your production environment. This is where hotfix branches start from.

If you happen to use continuous delivery for the development, QA and production environments, `develop`, `qa` and `production` are also good alternatives for these names, but keep in mind that forked repositories will not keep the CD system configured.

## GIT Conventions

### 1. Commit messages

**1.1.** Use commit messages in the present imperative tense.

Commits are an indication of which changes will be applied when the commit is introduced in a codebase. As such, it should answer the question: "What will this commit do?" "It will _fix the bug_" or "_refactor the code_", etc.

This also follows the standard for automatic messages created from git ("Merge branch..."), it preserves consistency against project tracking systems (which are almost always in the present imperative tense too) and it is a good suggestion for systems like GitHub and BitBucket to suggest a PR name when they use this message.

Bad examples:

- `Updating styles`
- `Updated styles`

Good examples:

- `Update styles`
- `Refactor mainComponent.js`

**1.2.** Limit your commit message to 80 characters in the first line.

Some tools, including console and GitHub are optimized for message sizes of about 70-80 characters. If you need to add further information (which is a good idea!), leave a blank line and add the rest later on. These next lines don't have the length limitation because they are only shown when long descriptions are expected.

Bad examples:

- `Refactor mainComponent.js to avoid memory leaks on the second $scope.$digest() loop`
- `Update mainComponent.js`

Good examples:

- Example 1:

    ```
    Refactor mainComponent.js

    Removed manual calls to $scope.$digest() to avoid events being invoked even when they already where unhooked.
    ```

- Example 2:

    ```
    Remove $scope.$digest() calls from mainComponent.js
    ```

**1.3.** Always include what changes you _did_ and not what you _intended to do_. Only time will tell if you achieved your real purpose, but the message will stay like that forever. Don't be afraid to include technical details -- this is for developers after all.

Bad examples:

- `Fix memory leaks`
- `Fix CFP-124`

Good examples:

- `Remove event handlers after use in main component loop`
- `Add cancellation token to async methods`

**1.4.** Final sentence period: Nope.

Bad example:

- `Remove event handlers after use in main component loop.`

Good example:

- `Remove event handlers after use in main component loop`

### 5. Merges

**5.1.** Avoid merge commits

A proper exception could be when a frozen branch (like a production branch) is back-merged into the development branch. In such a case, you don't want to alter the production branch at all. For all other merges of the day to day development, look to have a linear code history.

Use `git merge --squash` or `git rebase -i` (and squash all commits).

**5.2.** Include the tracking ticket number in the resulting squashed commit

Make sure the message of the last commit has the ticket number of the tracking change. This helps both finding a commit knowing the request, and finding the original request knowing what the change (commit) is.

Bad examples:

- `Fix memory leak`
- `Update main page styles`

Good examples:

- `[CFP-114] Remove unused event handlers`
- `[CFP-110] Update main page styles`

**5.2.1.** It is ok to have multiple commits for the same ticket. It may have taken several tries to achieve it, or it may just be very complicated for a single commit. Prioritize readability and traceability over having less commits.


## Third-Party systems used

To be completed

## App setup / Tests

To be completed after